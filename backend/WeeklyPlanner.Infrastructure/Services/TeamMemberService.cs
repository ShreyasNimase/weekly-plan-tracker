using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Core.Services;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Services;

public class TeamMemberService : ITeamMemberService
{
    private readonly ITeamMemberRepository _repo;
    private readonly AppDbContext _context;

    public TeamMemberService(ITeamMemberRepository repo, AppDbContext context)
    {
        _repo = repo;
        _context = context;
    }

    public async Task<(TeamMemberDto? Result, string? Error)> CreateAsync(CreateTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            return (null, "Name is required.");
        if (name.Length > 100)
            return (null, "Name cannot exceed 100 characters.");

        var allActive = await _repo.GetAllActiveAsync(cancellationToken);
        if (allActive.Any(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return (null, "Another active member already has this name.");

        var all = await _repo.GetAllAsync(cancellationToken);
        var isFirst = !all.Any();

        var member = new TeamMember
        {
            Name = name,
            IsLead = isFirst,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.AddAsync(member, cancellationToken);
        return (ToDto(created), null);
    }

    public async Task<IReadOnlyList<TeamMemberDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetAllAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<TeamMemberDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var m = await _repo.GetByIdAsync(id, cancellationToken);
        return m is null ? null : ToDto(m);
    }

    public async Task<(TeamMemberDto? Result, string? Error)> UpdateAsync(Guid id, UpdateTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            return (null, "Name is required.");
        if (name.Length > 100)
            return (null, "Name cannot exceed 100 characters.");

        var member = await _repo.GetByIdAsync(id, cancellationToken);
        if (member is null)
            return (null, "Team member not found.");

        var all = await _repo.GetAllAsync(cancellationToken);
        if (all.Any(m => m.Id != id && m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return (null, "Another member already has this name.");

        member.Name = name;
        await _repo.UpdateAsync(member, cancellationToken);
        return (ToDto(member), null);
    }

    public async Task<(TeamMemberDto? Result, string? Error)> MakeLeadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _repo.GetByIdAsync(id, cancellationToken);
        if (member is null)
            return (null, "Team member not found.");
        if (!member.IsActive)
            return (null, "Cannot make an inactive member the lead.");

        // Atomically clear all leads then set this member as lead
        await _context.TeamMembers
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsLead, false), cancellationToken);

        member.IsLead = true;
        await _repo.UpdateAsync(member, cancellationToken);
        return (ToDto(member), null);
    }

    public async Task<(TeamMemberDto? Result, string? Error)> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _repo.GetByIdAsync(id, cancellationToken);
        if (member is null)
            return (null, "Team member not found.");
        if (!member.IsActive)
            return (null, "Member is already inactive.");

        // Guard: cannot deactivate the Team Lead — must transfer lead first
        if (member.IsLead)
            return (null, "Cannot deactivate the Team Lead. Please assign a new lead first.");

        if (await _repo.IsInActiveCycleAsync(id, cancellationToken))
            return (null, "This person is part of an active plan right now.");

        member.IsActive = false;
        await _repo.UpdateAsync(member, cancellationToken);
        return (ToDto(member), null);
    }

    public async Task<(TeamMemberDto? Result, string? Error)> ReactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _repo.GetByIdAsync(id, cancellationToken);
        if (member is null)
            return (null, "Team member not found.");
        if (member.IsActive)
            return (null, "Member is already active.");

        member.IsActive = true;
        await _repo.UpdateAsync(member, cancellationToken);
        return (ToDto(member), null);
    }

    private static TeamMemberDto ToDto(TeamMember m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        IsLead = m.IsLead,
        IsActive = m.IsActive,
        CreatedAt = m.CreatedAt
    };
}

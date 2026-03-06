using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

/// <summary>
/// Repository for team members. Call <see cref="IUnitOfWork.SaveChangesAsync"/> after add/update.
/// </summary>
public class TeamMemberRepository : ITeamMemberRepository
{
    private readonly AppDbContext _context;

    public TeamMemberRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TeamMember> AddAsync(TeamMember teamMember, CancellationToken cancellationToken = default)
    {
        _context.TeamMembers.Add(teamMember);
        await _context.SaveChangesAsync(cancellationToken);
        return teamMember;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TeamMember>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TeamMembers
            .Where(m => m.IsActive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TeamMember>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TeamMembers
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TeamMembers.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TeamMember> UpdateAsync(TeamMember teamMember, CancellationToken cancellationToken = default)
    {
        _context.TeamMembers.Update(teamMember);
        await _context.SaveChangesAsync(cancellationToken);
        return teamMember;
    }

    /// <inheritdoc />
    public async Task<bool> IsInActiveCycleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CycleMembers
            .AnyAsync(cm =>
                cm.MemberId == id &&
                cm.Cycle.State != "COMPLETED",
                cancellationToken);
    }
}

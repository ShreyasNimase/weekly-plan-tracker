using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

public class TeamMemberRepository : ITeamMemberRepository
{
    private readonly AppDbContext _context;

    public TeamMemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TeamMember> AddAsync(TeamMember teamMember)
    {
        _context.TeamMembers.Add(teamMember);
        await _context.SaveChangesAsync();
        return teamMember;
    }

    public async Task<IEnumerable<TeamMember>> GetAllActiveAsync()
    {
        return await _context.TeamMembers
            .Where(m => m.IsActive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TeamMember>> GetAllAsync()
    {
        return await _context.TeamMembers
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<TeamMember?> GetByIdAsync(Guid id)
    {
        return await _context.TeamMembers.FindAsync(id);
    }

    public async Task<TeamMember> UpdateAsync(TeamMember teamMember)
    {
        _context.TeamMembers.Update(teamMember);
        await _context.SaveChangesAsync();
        return teamMember;
    }

    /// <summary>
    /// Returns true if the member is part of any cycle that is not yet Completed or Cancelled.
    /// Used by Deactivate endpoint to prevent removing active participants.
    /// </summary>
    public async Task<bool> IsInActiveCycleAsync(Guid id)
    {
        return await _context.CycleMembers
            .AnyAsync(cm =>
                cm.TeamMemberId == id &&
                cm.Cycle.Status != WeeklyPlanner.Core.Enums.CycleStatus.Completed &&
                cm.Cycle.Status != WeeklyPlanner.Core.Enums.CycleStatus.Cancelled);
    }
}


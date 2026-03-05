using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly AppDbContext _context;

    public AssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskAssignment> AddAsync(TaskAssignment assignment)
    {
        _context.TaskAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<TaskAssignment?> GetByIdAsync(Guid id)
    {
        return await _context.TaskAssignments
            .Include(a => a.CycleMember)
                .ThenInclude(cm => cm.Cycle)
            .Include(a => a.BacklogItem)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<TaskAssignment> UpdateAsync(TaskAssignment assignment)
    {
        if (_context.Entry(assignment).State == EntityState.Detached)
            _context.TaskAssignments.Update(assignment);

        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task DeleteAsync(Guid id)
    {
        var assignment = await _context.TaskAssignments.FindAsync(id);
        if (assignment is not null)
        {
            _context.TaskAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTotalHoursForMemberAsync(Guid cycleMemberId)
    {
        return await _context.TaskAssignments
            .Where(a => a.CycleMemberId == cycleMemberId)
            .SumAsync(a => a.PlannedHours);
    }

    public async Task<decimal> GetCategoryHoursUsedAsync(Guid cycleId, string category)
    {
        // Join through CycleMember to get the cycleId, then filter by BacklogItem.Category
        if (!Enum.TryParse<BacklogCategory>(category, out var cat))
            return 0m;

        return await _context.TaskAssignments
            .Where(a =>
                a.CycleMember.CycleId == cycleId &&
                a.BacklogItem.Category == cat)
            .SumAsync(a => a.PlannedHours);
    }

    public async Task<bool> IsBacklogItemClaimedInActiveCycleAsync(
        Guid backlogItemId,
        Guid excludeAssignmentId)
    {
        return await _context.TaskAssignments
            .AnyAsync(a =>
                a.BacklogItemId == backlogItemId &&
                a.Id != excludeAssignmentId &&
                a.CycleMember.Cycle.Status != CycleStatus.Completed &&
                a.CycleMember.Cycle.Status != CycleStatus.Cancelled);
    }

    public async Task<CycleMember?> GetCycleMemberByIdAsync(Guid cycleMemberId)
    {
        return await _context.CycleMembers
            .Include(cm => cm.Cycle)
            .Include(cm => cm.TeamMember)
            .Include(cm => cm.TaskAssignments)
                .ThenInclude(a => a.BacklogItem)
            .FirstOrDefaultAsync(cm => cm.Id == cycleMemberId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TaskAssignment>> GetMemberAssignmentsAsync(Guid cycleMemberId)
    {
        return await _context.TaskAssignments
            .Include(a => a.BacklogItem)
            .Include(a => a.CycleMember)
                .ThenInclude(cm => cm.TeamMember)
            .Where(a => a.CycleMemberId == cycleMemberId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskAssignment>> GetCycleAssignmentsAsync(Guid cycleId)
    {
        return await _context.TaskAssignments
            .Include(a => a.BacklogItem)
            .Include(a => a.CycleMember)
                .ThenInclude(cm => cm.TeamMember)
            .Where(a => a.CycleMember.CycleId == cycleId)
            .OrderBy(a => a.CycleMember.TeamMemberId)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();
    }
}

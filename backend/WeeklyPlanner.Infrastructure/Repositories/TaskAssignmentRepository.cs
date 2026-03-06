using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

/// <summary>
/// Repository for task assignments. Call <see cref="IUnitOfWork.SaveChangesAsync"/> after add/update/delete.
/// </summary>
public class TaskAssignmentRepository : ITaskAssignmentRepository
{
    private readonly AppDbContext _context;

    public TaskAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TaskAssignment> AddAsync(TaskAssignment assignment, CancellationToken cancellationToken = default)
    {
        _context.TaskAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    /// <inheritdoc />
    public async Task<TaskAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignments
            .Include(a => a.MemberPlan!)
            .ThenInclude(mp => mp.Cycle!)
            .ThenInclude(c => c.CategoryAllocations)
            .Include(a => a.BacklogItem)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TaskAssignment> UpdateAsync(TaskAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(assignment).State == EntityState.Detached)
            _context.TaskAssignments.Update(assignment);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.TaskAssignments.FindAsync([id], cancellationToken);
        if (assignment is not null)
        {
            _context.TaskAssignments.Remove(assignment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalHoursForMemberPlanAsync(Guid memberPlanId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignments
            .Where(a => a.MemberPlanId == memberPlanId)
            .SumAsync(a => a.CommittedHours, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetCategoryHoursUsedAsync(Guid cycleId, string category, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignments
            .Where(a =>
                a.MemberPlan!.CycleId == cycleId &&
                a.BacklogItem!.Category == category)
            .SumAsync(a => a.CommittedHours, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsBacklogItemClaimedInActiveCycleAsync(Guid backlogItemId, Guid? excludeAssignmentId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignments
            .AnyAsync(a =>
                a.BacklogItemId == backlogItemId &&
                (excludeAssignmentId == null || a.Id != excludeAssignmentId) &&
                a.MemberPlan!.Cycle!.State != "COMPLETED",
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskAssignment>> GetByMemberPlanIdAsync(Guid memberPlanId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignments
            .Include(a => a.BacklogItem)
            .Where(a => a.MemberPlanId == memberPlanId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TaskAssignment>> GetByCycleIdAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignments
            .Include(a => a.BacklogItem)
            .Include(a => a.MemberPlan!)
            .ThenInclude(mp => mp.Member)
            .Where(a => a.MemberPlan!.CycleId == cycleId)
            .OrderBy(a => a.MemberPlan!.MemberId)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

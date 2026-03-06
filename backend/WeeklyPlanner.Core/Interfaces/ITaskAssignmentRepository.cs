using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Repository for task assignments (backlog item committed to a member plan).
/// </summary>
public interface ITaskAssignmentRepository
{
    /// <summary>Adds a new task assignment.</summary>
    Task<TaskAssignment> AddAsync(TaskAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Gets a task assignment by id with MemberPlan, BacklogItem.</summary>
    Task<TaskAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing task assignment.</summary>
    Task<TaskAssignment> UpdateAsync(TaskAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Deletes a task assignment by id (cascades to ProgressUpdates).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sum of CommittedHours for all assignments of a member plan.</summary>
    Task<decimal> GetTotalHoursForMemberPlanAsync(Guid memberPlanId, CancellationToken cancellationToken = default);

    /// <summary>Sum of CommittedHours for a category within a cycle (via MemberPlan and BacklogItem).</summary>
    Task<decimal> GetCategoryHoursUsedAsync(Guid cycleId, string category, CancellationToken cancellationToken = default);

    /// <summary>True if this BacklogItem is already assigned in any non-completed cycle, optionally excluding an assignment id.</summary>
    Task<bool> IsBacklogItemClaimedInActiveCycleAsync(Guid backlogItemId, Guid? excludeAssignmentId, CancellationToken cancellationToken = default);

    /// <summary>All assignments for a member plan (for member progress view).</summary>
    Task<IEnumerable<TaskAssignment>> GetByMemberPlanIdAsync(Guid memberPlanId, CancellationToken cancellationToken = default);

    /// <summary>All assignments for a cycle (across all member plans).</summary>
    Task<IEnumerable<TaskAssignment>> GetByCycleIdAsync(Guid cycleId, CancellationToken cancellationToken = default);
}

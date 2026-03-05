using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

public interface IAssignmentRepository
{
    Task<TaskAssignment> AddAsync(TaskAssignment assignment);
    Task<TaskAssignment?> GetByIdAsync(Guid id);
    Task<TaskAssignment> UpdateAsync(TaskAssignment assignment);
    Task DeleteAsync(Guid id);

    /// <summary>Sum of PlannedHours for all assignments belonging to a CycleMember.</summary>
    Task<decimal> GetTotalHoursForMemberAsync(Guid cycleMemberId);

    /// <summary>Sum of PlannedHours for a category within a cycle.</summary>
    Task<decimal> GetCategoryHoursUsedAsync(Guid cycleId, string category);

    /// <summary>True if this BacklogItem is already assigned in any non-completed cycle.</summary>
    Task<bool> IsBacklogItemClaimedInActiveCycleAsync(Guid backlogItemId, Guid excludeAssignmentId);

    /// <summary>Get CycleMember by its Id (for plan-ready validation).</summary>
    Task<CycleMember?> GetCycleMemberByIdAsync(Guid cycleMemberId);

    /// <summary>Save changes after modifying a CycleMember directly.</summary>
    Task SaveChangesAsync();

    /// <summary>All assignments for a specific CycleMember (for member progress view).</summary>
    Task<IEnumerable<TaskAssignment>> GetMemberAssignmentsAsync(Guid cycleMemberId);

    /// <summary>All assignments across the entire cycle (for cycle progress view).</summary>
    Task<IEnumerable<TaskAssignment>> GetCycleAssignmentsAsync(Guid cycleId);
}

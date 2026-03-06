using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Repository for planning cycles. State: SETUP, PLANNING, FROZEN, COMPLETED.
/// </summary>
public interface ICycleRepository
{
    /// <summary>Adds a new planning cycle.</summary>
    Task<PlanningCycle> AddAsync(PlanningCycle cycle, CancellationToken cancellationToken = default);

    /// <summary>Gets the active cycle (State not COMPLETED).</summary>
    Task<PlanningCycle?> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a cycle by id with CycleMembers, CategoryAllocations, MemberPlans.</summary>
    Task<PlanningCycle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing cycle.</summary>
    Task<PlanningCycle> UpdateAsync(PlanningCycle cycle, CancellationToken cancellationToken = default);

    /// <summary>Deletes a cycle by id (cascades to CycleMembers, CategoryAllocations, MemberPlans).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets past cycles (State = COMPLETED) ordered by PlanningDate descending.</summary>
    Task<IEnumerable<PlanningCycle>> GetHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if any cycle exists that is not COMPLETED.</summary>
    Task<bool> HasActiveCycleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all CycleMembers and CategoryAllocations for the cycle.
    /// Caller is responsible for setting CycleId on members and allocations.
    /// </summary>
    Task SetupMembersAndAllocationsAsync(PlanningCycle cycle, List<CycleMember> members, List<CategoryAllocation> allocations, CancellationToken cancellationToken = default);
}

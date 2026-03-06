using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Repository for member plans (per-cycle member planning and readiness).
/// </summary>
public interface IMemberPlanRepository
{
    /// <summary>Adds a new member plan.</summary>
    Task<MemberPlan> AddAsync(MemberPlan memberPlan, CancellationToken cancellationToken = default);

    /// <summary>Gets a member plan by id with TaskAssignments and BacklogItem.</summary>
    Task<MemberPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets the member plan for a given cycle and member, if any.</summary>
    Task<MemberPlan?> GetByCycleAndMemberAsync(Guid cycleId, Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Gets all member plans for a cycle with Member and TaskAssignments.</summary>
    Task<IEnumerable<MemberPlan>> GetByCycleIdAsync(Guid cycleId, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing member plan.</summary>
    Task<MemberPlan> UpdateAsync(MemberPlan memberPlan, CancellationToken cancellationToken = default);

    /// <summary>Deletes a member plan by id (cascades to TaskAssignments).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deletes all member plans for a cycle (cascades to TaskAssignments).</summary>
    Task DeleteByCycleIdAsync(Guid cycleId, CancellationToken cancellationToken = default);
}

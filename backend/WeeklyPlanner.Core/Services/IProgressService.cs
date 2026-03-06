using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for progress updates and progress views.</summary>
public interface IProgressService
{
    /// <summary>Updates assignment progress (cycle must be FROZEN). Enforces status transitions and creates ProgressUpdate.</summary>
    Task<(AssignmentDto? Result, string? Error)> UpdateProgressAsync(Guid assignmentId, UpdateProgressRequest request, Guid? currentUserId, CancellationToken cancellationToken = default);

    /// <summary>All member plans and assignments for a cycle.</summary>
    Task<CycleProgressDto?> GetCycleProgressAsync(Guid cycleId, CancellationToken cancellationToken = default);

    /// <summary>Single member's plan and assignments with full ProgressUpdate history.</summary>
    Task<MemberProgressDetailDto?> GetMemberProgressAsync(Guid cycleId, Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Per-category progress (budgetHours, totalCommitted, totalCompleted, counts).</summary>
    Task<IReadOnlyList<CategoryProgressDto>?> GetCategoryProgressAsync(Guid cycleId, CancellationToken cancellationToken = default);
}

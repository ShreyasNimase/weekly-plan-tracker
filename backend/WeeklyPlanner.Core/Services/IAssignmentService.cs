using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for task assignment operations.</summary>
public interface IAssignmentService
{
    /// <summary>Creates an assignment. Validates 30h cap and category budget. Sets BacklogItem IN_PLAN, updates MemberPlan.</summary>
    Task<(AssignmentDto? Result, string? Error)> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates committed hours. Validates 30h cap and category budget. Updates MemberPlan, sets IsReady=false.</summary>
    Task<(AssignmentDto? Result, string? Error)> UpdateAsync(Guid assignmentId, UpdateAssignmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes an assignment. Sets BacklogItem AVAILABLE if no other in cycle. Updates MemberPlan.</summary>
    Task<string?> DeleteAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Gets an assignment by id.</summary>
    Task<AssignmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

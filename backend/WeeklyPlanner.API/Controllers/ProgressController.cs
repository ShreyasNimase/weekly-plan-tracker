using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>Progress update and progress view APIs.</summary>
[ApiController]
[Route("api")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;

    public ProgressController(IProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>Updates assignment progress (cycle must be FROZEN). Enforces status transitions; creates ProgressUpdate audit.</summary>
    /// <param name="id">Assignment id.</param>
    /// <param name="request">ProgressStatus, HoursCompleted, optional Note.</param>
    /// <param name="currentUserId">Optional; from header X-Current-User-Id or query currentUserId.</param>
    [HttpPut("assignments/{id:guid}/progress")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressRequest request, [FromHeader(Name = "X-Current-User-Id")] Guid? currentUserId, [FromQuery] Guid? currentUserIdQuery, CancellationToken cancellationToken = default)
    {
        var userId = currentUserId ?? currentUserIdQuery;
        var (result, error) = await _progressService.UpdateProgressAsync(id, request, userId, cancellationToken);
        if (error == "Assignment not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>All member plans and assignments for a cycle.</summary>
    [HttpGet("cycles/{id:guid}/progress")]
    [ProducesResponseType(typeof(CycleProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCycleProgress(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _progressService.GetCycleProgressAsync(id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Cycle not found." });
        return Ok(result);
    }

    /// <summary>Single member's plan and assignments with full ProgressUpdate history (by member id).</summary>
    [HttpGet("cycles/{id:guid}/members/{memberId:guid}/progress")]
    [ProducesResponseType(typeof(MemberProgressDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMemberProgress(Guid id, Guid memberId, CancellationToken cancellationToken = default)
    {
        var result = await _progressService.GetMemberProgressAsync(id, memberId, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Member not in this cycle." });
        return Ok(result);
    }

    /// <summary>Per-category progress: budgetHours, totalCommitted, totalCompleted, counts.</summary>
    [HttpGet("cycles/{id:guid}/category-progress")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryProgress(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _progressService.GetCategoryProgressAsync(id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Cycle not found." });
        return Ok(result);
    }
}

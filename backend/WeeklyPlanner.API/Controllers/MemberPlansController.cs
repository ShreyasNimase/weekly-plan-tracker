using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

/// <summary>APIs for member plans (per-cycle readiness).</summary>
[ApiController]
[Route("api/member-plans")]
public class MemberPlansController : ControllerBase
{
    private readonly IMemberPlanRepository _memberPlans;

    public MemberPlansController(IMemberPlanRepository memberPlans)
    {
        _memberPlans = memberPlans;
    }

    /// <summary>Toggles IsReady (true→false, false→true). Cycle must be in PLANNING state.</summary>
    /// <response code="200">Updated MemberPlan.</response>
    /// <response code="400">Cycle not in PLANNING.</response>
    /// <response code="404">Member plan not found.</response>
    [HttpPut("{id:guid}/ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleReady(Guid id, CancellationToken cancellationToken = default)
    {
        var memberPlan = await _memberPlans.GetByIdAsync(id, cancellationToken);
        if (memberPlan is null)
            return NotFound(new { message = "Member plan not found." });
        if (memberPlan.Cycle?.State != "PLANNING")
            return BadRequest(new { message = "Cycle must be in PLANNING state to toggle ready." });

        memberPlan.IsReady = !memberPlan.IsReady;
        await _memberPlans.UpdateAsync(memberPlan, cancellationToken);

        return Ok(new
        {
            memberPlan.Id,
            memberPlan.MemberId,
            memberPlan.IsReady,
            memberPlan.TotalPlannedHours
        });
    }
}

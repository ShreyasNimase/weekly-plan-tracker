using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/member-plans")]
public class MemberPlansController : ControllerBase
{
    private readonly IAssignmentRepository _repo;

    public MemberPlansController(IAssignmentRepository repo)
    {
        _repo = repo;
    }

    // ─────────────────────────────────────────────
    // 24. PUT /api/member-plans/{id}/ready
    //     {id} = CycleMember.Id
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/ready")]
    public async Task<IActionResult> MarkReady(Guid id)
    {
        var cycleMember = await _repo.GetCycleMemberByIdAsync(id);
        if (cycleMember is null)
            return NotFound(new { message = $"Member plan '{id}' not found." });

        if (cycleMember.Cycle.Status != CycleStatus.Planning)
            return BadRequest(new { message = $"Cannot mark ready — cycle is in '{cycleMember.Cycle.Status}' state. Must be Planning." });

        if (!cycleMember.TaskAssignments.Any())
            return BadRequest(new { message = "Cannot mark plan ready — no tasks assigned yet." });

        var totalPlanned = cycleMember.TaskAssignments.Sum(a => a.PlannedHours);
        if (totalPlanned > 30m)
            return BadRequest(new { message = $"Total planned hours ({totalPlanned}h) exceed the 30h cap." });

        if (cycleMember.IsReady)
            return BadRequest(new { message = "Plan is already marked as ready." });

        cycleMember.IsReady = true;
        await _repo.SaveChangesAsync();

        return Ok(new
        {
            CycleMemberId  = cycleMember.Id,
            TeamMemberName = cycleMember.TeamMember.Name,
            IsReady        = cycleMember.IsReady,
            TotalPlanned   = totalPlanned,
            Assignments    = cycleMember.TaskAssignments.Select(a => new
            {
                a.Id,
                a.BacklogItemId,
                BacklogItemTitle = a.BacklogItem.Title,
                a.PlannedHours
            })
        });
    }
}

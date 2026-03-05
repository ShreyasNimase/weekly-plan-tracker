using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/cycles")]
public class CyclesController : ControllerBase
{
    private readonly ICycleRepository _cycles;
    private readonly ITeamMemberRepository _members;

    public CyclesController(ICycleRepository cycles, ITeamMemberRepository members)
    {
        _cycles  = cycles;
        _members = members;
    }

    // ─────────────────────────────────────────────
    // 14. POST /api/cycles/start — Start New Week
    // ─────────────────────────────────────────────
    [HttpPost("start")]
    public async Task<IActionResult> StartCycle([FromBody] StartCycleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // ⚠ Business rule: week start must be a Tuesday
        if (dto.WeekStartDate.DayOfWeek != DayOfWeek.Tuesday)
            return BadRequest(new { message = "Week start date must be a Tuesday." });

        // ⚠ Business rule: only one active cycle at a time
        if (await _cycles.HasActiveCycleAsync())
            return BadRequest(new { message = "An active planning cycle already exists. Complete or cancel it before starting a new one." });

        var cycle = new PlanningCycle
        {
            WeekStartDate = dto.WeekStartDate.Date,
            Status        = CycleStatus.Setup,
            CreatedAt     = DateTime.UtcNow
        };

        var created = await _cycles.AddAsync(cycle);
        return CreatedAtAction(nameof(GetActiveCycle), null, ToCycleResponse(created));
    }

    // ─────────────────────────────────────────────
    // 15. PUT /api/cycles/{id}/setup — Setup Members + Budgets
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/setup")]
    public async Task<IActionResult> SetupCycle(Guid id, [FromBody] SetupCycleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        if (cycle.Status != CycleStatus.Setup)
            return BadRequest(new { message = $"Cannot configure a cycle in '{cycle.Status}' state. Only Setup cycles can be configured." });

        // ⚠ Percentage must sum to exactly 100
        var totalPct = dto.CategoryBudgets.Sum(b => b.Percentage);
        if (Math.Round(totalPct, 2) != 100m)
            return BadRequest(new { message = $"Category percentages must total 100%. Current total: {totalPct}%." });

        // ⚠ All member IDs must exist and be active
        var invalidMembers = new List<Guid>();
        foreach (var memberId in dto.MemberIds.Distinct())
        {
            var m = await _members.GetByIdAsync(memberId);
            if (m is null || !m.IsActive)
                invalidMembers.Add(memberId);
        }
        if (invalidMembers.Count > 0)
            return BadRequest(new { message = "One or more member IDs are invalid or inactive.", invalidIds = invalidMembers });

        int memberCount = dto.MemberIds.Distinct().Count();
        decimal totalHours = memberCount * 30m;

        // Build replacement lists (repository handles the old-removal safely)
        var newMembers = dto.MemberIds.Distinct().Select(memberId => new CycleMember
        {
            TeamMemberId   = memberId,
            AllocatedHours = 30m
        }).ToList();

        var newBudgets = dto.CategoryBudgets.Select(budget => new CategoryBudget
        {
            Category    = budget.Category,
            Percentage  = budget.Percentage,
            HoursBudget = Math.Round(totalHours * budget.Percentage / 100m, 2)
        }).ToList();

        await _cycles.SetupMembersAndBudgetsAsync(cycle, newMembers, newBudgets);
        return Ok(ToCycleResponse(cycle));
    }

    // ─────────────────────────────────────────────
    // 16. PUT /api/cycles/{id}/open — Open Planning
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/open")]
    public async Task<IActionResult> OpenPlanning(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        if (cycle.Status != CycleStatus.Setup)
            return BadRequest(new { message = $"Cannot open planning from '{cycle.Status}' state. Cycle must be in Setup." });

        if (!cycle.CycleMembers.Any())
            return BadRequest(new { message = "Cannot open planning without any members. Run setup first." });

        if (!cycle.CategoryBudgets.Any())
            return BadRequest(new { message = "Cannot open planning without category budgets. Run setup first." });

        cycle.Status = CycleStatus.Planning;
        await _cycles.UpdateAsync(cycle);
        return Ok(ToCycleResponse(cycle));
    }

    // ─────────────────────────────────────────────
    // 17. GET /api/cycles/active — Get Active Cycle
    // ─────────────────────────────────────────────
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCycle()
    {
        var cycle = await _cycles.GetActiveAsync();
        if (cycle is null)
            return NotFound(new { message = "No active planning cycle exists." });

        return Ok(ToCycleResponse(cycle));
    }

    // ─────────────────────────────────────────────
    // 18. PUT /api/cycles/{id}/freeze — Freeze Plan
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/freeze")]
    public async Task<IActionResult> FreezePlan(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        if (cycle.Status != CycleStatus.Planning)
            return BadRequest(new { message = $"Cannot freeze from '{cycle.Status}' state. Cycle must be in Planning." });

        if (!cycle.CycleMembers.Any())
            return BadRequest(new { message = "Cannot freeze: no members in cycle." });

        // ⚠ Validate category % still sums to 100
        var totalPct = cycle.CategoryBudgets.Sum(b => b.Percentage);
        if (Math.Round(totalPct, 2) != 100m)
            return BadRequest(new { message = $"Cannot freeze: category percentages total {totalPct}%, must be 100%." });

        // NOTE: Full "each member = 30h" validation wires in once Member Planning APIs are done (Phase 4)

        cycle.Status = CycleStatus.Frozen;
        await _cycles.UpdateAsync(cycle);
        return Ok(ToCycleResponse(cycle));
    }

    // ─────────────────────────────────────────────
    // 19. PUT /api/cycles/{id}/complete — Finish Week
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> FinishWeek(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        if (cycle.Status != CycleStatus.Frozen)
            return BadRequest(new { message = $"Cannot complete from '{cycle.Status}' state. Cycle must be Frozen first." });

        cycle.Status = CycleStatus.Completed;
        await _cycles.UpdateAsync(cycle);
        return Ok(ToCycleResponse(cycle));
    }

    // ─────────────────────────────────────────────
    // 20. DELETE /api/cycles/{id} — Cancel Planning
    // ─────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelCycle(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        if (cycle.Status == CycleStatus.Completed)
            return BadRequest(new { message = "Cannot cancel a completed cycle." });

        if (cycle.Status == CycleStatus.Cancelled)
            return BadRequest(new { message = "Cycle is already cancelled." });

        await _cycles.DeleteAsync(id);

        // Re-fetch to return updated state
        var updated = await _cycles.GetByIdAsync(id);
        return Ok(ToCycleResponse(updated!));
    }

    // ─────────────────────────────────────────────
    // BONUS: GET /api/cycles/history
    // ─────────────────────────────────────────────
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _cycles.GetHistoryAsync();
        return Ok(history.Select(ToCycleResponse));
    }

    // ─────────────────────────────────────────────
    // Shared response shaping
    // ─────────────────────────────────────────────
    private static object ToCycleResponse(PlanningCycle c) => new
    {
        c.Id,
        WeekStartDate  = c.WeekStartDate.ToString("yyyy-MM-dd"),
        Status         = c.Status.ToString(),
        c.CreatedAt,
        Members = c.CycleMembers.Select(cm => new
        {
            cm.TeamMemberId,
            cm.TeamMember?.Name,
            cm.AllocatedHours
        }),
        CategoryBudgets = c.CategoryBudgets.Select(cb => new
        {
            Category    = cb.Category.ToString(),
            cb.Percentage,
            cb.HoursBudget
        })
    };
}

using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentRepository _repo;
    private readonly ICycleRepository      _cycles;
    private readonly IBacklogRepository    _backlog;

    public AssignmentsController(
        IAssignmentRepository repo,
        ICycleRepository cycles,
        IBacklogRepository backlog)
    {
        _repo    = repo;
        _cycles  = cycles;
        _backlog = backlog;
    }

    // ─────────────────────────────────────────────
    // 21. POST /api/assignments — Claim Backlog Item
    // ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ClaimItem([FromBody] ClaimBacklogItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // ⚠ 0.5-hour increments only
        if (dto.PlannedHours % 0.5m != 0)
            return BadRequest(new { message = "Planned hours must be in 0.5-hour increments (e.g. 0.5, 1.0, 1.5)." });

        // ⚠ Validate CycleMember exists and its cycle is in Planning state
        var cycleMember = await _repo.GetCycleMemberByIdAsync(dto.CycleMemberId);
        if (cycleMember is null)
            return NotFound(new { message = $"CycleMember '{dto.CycleMemberId}' not found." });

        if (cycleMember.Cycle.Status != CycleStatus.Planning)
            return BadRequest(new { message = $"Cannot claim items — cycle is in '{cycleMember.Cycle.Status}' state. Cycle must be in Planning." });

        // ⚠ BacklogItem must exist and be Active
        var backlogItem = await _backlog.GetByIdAsync(dto.BacklogItemId);
        if (backlogItem is null)
            return NotFound(new { message = $"BacklogItem '{dto.BacklogItemId}' not found." });

        if (backlogItem.Status == BacklogStatus.Archived)
            return BadRequest(new { message = "Cannot assign an archived backlog item." });

        // ⚠ BacklogItem not already claimed in another active cycle
        var alreadyClaimed = await _repo.IsBacklogItemClaimedInActiveCycleAsync(dto.BacklogItemId, Guid.Empty);
        if (alreadyClaimed)
            return BadRequest(new { message = "This backlog item is already assigned in an active cycle." });

        // ⚠ Member 30h cap
        var usedHours = await _repo.GetTotalHoursForMemberAsync(dto.CycleMemberId);
        if (usedHours + dto.PlannedHours > 30m)
            return BadRequest(new { message = $"Cannot assign {dto.PlannedHours}h — member already has {usedHours}h planned (cap is 30h)." });

        // ⚠ Category budget cap
        var categoryName  = backlogItem.Category.ToString();
        var categoryUsed  = await _repo.GetCategoryHoursUsedAsync(cycleMember.CycleId, categoryName);
        var budget        = cycleMember.Cycle.CategoryBudgets
                                .FirstOrDefault(b => b.Category == backlogItem.Category);
        if (budget is not null && categoryUsed + dto.PlannedHours > budget.HoursBudget)
            return BadRequest(new
            {
                message   = $"Category '{categoryName}' budget exceeded. Budget: {budget.HoursBudget}h, Used: {categoryUsed}h, Requested: {dto.PlannedHours}h."
            });

        var assignment = new TaskAssignment
        {
            CycleMemberId = dto.CycleMemberId,
            BacklogItemId = dto.BacklogItemId,
            PlannedHours  = dto.PlannedHours,
            CreatedAt     = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(assignment);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    // ─────────────────────────────────────────────
    // 22. PUT /api/assignments/{id} — Update Hours
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateHours(Guid id, [FromBody] UpdateAssignmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.PlannedHours % 0.5m != 0)
            return BadRequest(new { message = "Planned hours must be in 0.5-hour increments." });

        var assignment = await _repo.GetByIdAsync(id);
        if (assignment is null)
            return NotFound(new { message = $"Assignment '{id}' not found." });

        if (assignment.CycleMember.Cycle.Status != CycleStatus.Planning)
            return BadRequest(new { message = $"Cannot update — cycle is in '{assignment.CycleMember.Cycle.Status}' state." });

        // ⚠ Re-check 30h cap excluding this assignment's current hours
        var usedHours = await _repo.GetTotalHoursForMemberAsync(assignment.CycleMemberId);
        var newTotal  = usedHours - assignment.PlannedHours + dto.PlannedHours;
        if (newTotal > 30m)
            return BadRequest(new { message = $"Update would exceed 30h cap. Current: {usedHours}h, New total would be: {newTotal}h." });

        // ⚠ Re-check category budget
        var categoryName = assignment.BacklogItem.Category.ToString();
        var categoryUsed = await _repo.GetCategoryHoursUsedAsync(
            assignment.CycleMember.CycleId, categoryName);
        var budget = assignment.CycleMember.Cycle.CategoryBudgets
                        .FirstOrDefault(b => b.Category == assignment.BacklogItem.Category);
        var newCatTotal = categoryUsed - assignment.PlannedHours + dto.PlannedHours;
        if (budget is not null && newCatTotal > budget.HoursBudget)
            return BadRequest(new
            {
                message = $"Update would exceed category '{categoryName}' budget of {budget.HoursBudget}h."
            });

        assignment.PlannedHours = dto.PlannedHours;
        await _repo.UpdateAsync(assignment);
        return Ok(ToResponse(assignment));
    }

    // ─────────────────────────────────────────────
    // 23. DELETE /api/assignments/{id} — Remove
    // ─────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveAssignment(Guid id)
    {
        var assignment = await _repo.GetByIdAsync(id);
        if (assignment is null)
            return NotFound(new { message = $"Assignment '{id}' not found." });

        if (assignment.CycleMember.Cycle.Status != CycleStatus.Planning)
            return BadRequest(new { message = $"Cannot remove — cycle is in '{assignment.CycleMember.Cycle.Status}' state." });

        await _repo.DeleteAsync(id);
        return NoContent();
    }

    // Internal helper for CreatedAtAction
    [HttpGet("{id:guid}", Name = "GetAssignmentById")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var a = await _repo.GetByIdAsync(id);
        return a is null ? NotFound() : Ok(ToResponse(a));
    }

    // ─── Response shaping ───────────────────────────────────────
    private static object ToResponse(TaskAssignment a) => new
    {
        a.Id,
        a.CycleMemberId,
        a.BacklogItemId,
        BacklogItemTitle    = a.BacklogItem?.Title,
        BacklogItemCategory = a.BacklogItem?.Category.ToString(),
        a.PlannedHours,
        a.CreatedAt
    };
}

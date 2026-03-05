using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/cycles")]
public class ProgressController : ControllerBase
{
    private readonly ICycleRepository      _cycles;
    private readonly IAssignmentRepository _assignments;

    public ProgressController(ICycleRepository cycles, IAssignmentRepository assignments)
    {
        _cycles      = cycles;
        _assignments = assignments;
    }

    // ─────────────────────────────────────────────
    // 25. GET /api/cycles/{id}/progress
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> GetCycleProgress(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        var allAssignments = (await _assignments.GetCycleAssignmentsAsync(id)).ToList();

        var totalPlanned = allAssignments.Sum(a => a.PlannedHours);
        var totalAllocated = cycle.CycleMembers.Count * 30m;

        var memberProgress = cycle.CycleMembers.Select(cm =>
        {
            var memberTasks   = allAssignments.Where(a => a.CycleMemberId == cm.Id).ToList();
            var memberPlanned = memberTasks.Sum(a => a.PlannedHours);
            return new
            {
                cycleMemberId  = cm.Id,
                teamMemberId   = cm.TeamMemberId,
                name           = cm.TeamMember?.Name,
                isReady        = cm.IsReady,
                allocatedHours = cm.AllocatedHours,
                plannedHours   = memberPlanned,
                remainingHours = cm.AllocatedHours - memberPlanned,
                taskCount      = memberTasks.Count
            };
        }).ToList();

        var categoryBreakdown = cycle.CategoryBudgets.Select(cb =>
        {
            var used = allAssignments
                .Where(a => a.BacklogItem.Category == cb.Category)
                .Sum(a => a.PlannedHours);
            return new
            {
                category     = cb.Category.ToString(),
                budgetHours  = cb.HoursBudget,
                usedHours    = used,
                utilization  = cb.HoursBudget > 0
                    ? Math.Round(used / cb.HoursBudget * 100, 1)
                    : 0m
            };
        }).ToList();

        return Ok(new
        {
            cycleId            = cycle.Id,
            weekStartDate      = cycle.WeekStartDate.ToString("yyyy-MM-dd"),
            status             = cycle.Status.ToString(),
            totalMembers       = cycle.CycleMembers.Count,
            readyMembers       = cycle.CycleMembers.Count(cm => cm.IsReady),
            totalAllocatedHours = totalAllocated,
            totalPlannedHours  = totalPlanned,
            utilizationPercent = totalAllocated > 0
                ? Math.Round(totalPlanned / totalAllocated * 100, 1)
                : 0m,
            members            = memberProgress,
            categoryBreakdown
        });
    }

    // ─────────────────────────────────────────────
    // 26. GET /api/cycles/{id}/members/{cycleMemberId}/progress
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}/members/{cycleMemberId:guid}/progress")]
    public async Task<IActionResult> GetMemberProgress(Guid id, Guid cycleMemberId)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        var cycleMember = cycle.CycleMembers.FirstOrDefault(cm => cm.Id == cycleMemberId);
        if (cycleMember is null)
            return NotFound(new { message = $"Member '{cycleMemberId}' not found in this cycle." });

        var assignments = (await _assignments.GetMemberAssignmentsAsync(cycleMemberId)).ToList();
        var planned     = assignments.Sum(a => a.PlannedHours);

        return Ok(new
        {
            cycleMemberId  = cycleMember.Id,
            teamMemberId   = cycleMember.TeamMemberId,
            name           = cycleMember.TeamMember?.Name,
            isReady        = cycleMember.IsReady,
            allocatedHours = cycleMember.AllocatedHours,
            plannedHours   = planned,
            remainingHours = cycleMember.AllocatedHours - planned,
            tasks = assignments.Select(a => new
            {
                assignmentId = a.Id,
                a.BacklogItemId,
                title        = a.BacklogItem.Title,
                category     = a.BacklogItem.Category.ToString(),
                priority     = a.BacklogItem.Priority.ToString(),
                a.PlannedHours
            })
        });
    }

    // ─────────────────────────────────────────────
    // 27. GET /api/cycles/{id}/category-progress
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}/category-progress")]
    public async Task<IActionResult> GetCategoryProgress(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        var allAssignments = (await _assignments.GetCycleAssignmentsAsync(id)).ToList();

        var breakdown = cycle.CategoryBudgets.Select(cb =>
        {
            var used = allAssignments
                .Where(a => a.BacklogItem.Category == cb.Category)
                .Sum(a => a.PlannedHours);
            return new
            {
                category     = cb.Category.ToString(),
                percentage   = cb.Percentage,
                budgetHours  = cb.HoursBudget,
                usedHours    = used,
                remaining    = cb.HoursBudget - used,
                utilization  = cb.HoursBudget > 0
                    ? Math.Round(used / cb.HoursBudget * 100, 1)
                    : 0m
            };
        });

        return Ok(breakdown);
    }

    // ─────────────────────────────────────────────
    // BONUS: GET /api/cycles/{id} — Single Cycle Details
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCycleById(Guid id)
    {
        var cycle = await _cycles.GetByIdAsync(id);
        if (cycle is null)
            return NotFound(new { message = $"Cycle '{id}' not found." });

        return Ok(new
        {
            cycle.Id,
            WeekStartDate   = cycle.WeekStartDate.ToString("yyyy-MM-dd"),
            Status          = cycle.Status.ToString(),
            cycle.CreatedAt,
            Members = cycle.CycleMembers.Select(cm => new
            {
                cm.Id,
                cm.TeamMemberId,
                cm.TeamMember?.Name,
                cm.AllocatedHours,
                cm.IsReady
            }),
            CategoryBudgets = cycle.CategoryBudgets.Select(cb => new
            {
                Category    = cb.Category.ToString(),
                cb.Percentage,
                cb.HoursBudget
            })
        });
    }
}

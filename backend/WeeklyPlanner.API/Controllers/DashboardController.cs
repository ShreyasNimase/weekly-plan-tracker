using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ICycleRepository      _cycles;
    private readonly ITeamMemberRepository _members;
    private readonly IBacklogRepository    _backlog;
    private readonly IAssignmentRepository _assignments;

    public DashboardController(
        ICycleRepository cycles,
        ITeamMemberRepository members,
        IBacklogRepository backlog,
        IAssignmentRepository assignments)
    {
        _cycles      = cycles;
        _members     = members;
        _backlog     = backlog;
        _assignments = assignments;
    }

    // ─────────────────────────────────────────────
    // 28. GET /api/dashboard
    // ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        // Active cycle snapshot
        var activeCycle = await _cycles.GetActiveAsync();
        object? activeCycleSnapshot = null;

        if (activeCycle is not null)
        {
            var cycleAssignments = (await _assignments.GetCycleAssignmentsAsync(activeCycle.Id)).ToList();
            activeCycleSnapshot = new
            {
                activeCycle.Id,
                WeekStartDate   = activeCycle.WeekStartDate.ToString("yyyy-MM-dd"),
                Status          = activeCycle.Status.ToString(),
                totalMembers    = activeCycle.CycleMembers.Count,
                readyMembers    = activeCycle.CycleMembers.Count(cm => cm.IsReady),
                totalAllocatedHours = activeCycle.CycleMembers.Count * 30m,
                totalPlannedHours   = cycleAssignments.Sum(a => a.PlannedHours),
                taskCount       = cycleAssignments.Count
            };
        }

        // Team summary
        var allMembers    = (await _members.GetAllAsync()).ToList();
        var activeMembers = allMembers.Where(m => m.IsActive).ToList();
        var lead          = activeMembers.FirstOrDefault(m => m.IsLead);

        var teamSummary = new
        {
            total     = allMembers.Count,
            active    = activeMembers.Count,
            inactive  = allMembers.Count - activeMembers.Count,
            leadName  = lead?.Name ?? "(no lead assigned)"
        };

        // Backlog summary
        var activeBacklog   = (await _backlog.GetAllAsync(null, BacklogStatus.Active,   null)).ToList();
        var archivedBacklog = (await _backlog.GetAllAsync(null, BacklogStatus.Archived, null)).ToList();

        var backlogSummary = new
        {
            total    = activeBacklog.Count + archivedBacklog.Count,
            active   = activeBacklog.Count,
            archived = archivedBacklog.Count,
            byPriority = new
            {
                high   = activeBacklog.Count(b => b.Priority == BacklogPriority.High),
                medium = activeBacklog.Count(b => b.Priority == BacklogPriority.Medium),
                low    = activeBacklog.Count(b => b.Priority == BacklogPriority.Low)
            }
        };

        // Recent history (last 3 completed/cancelled)
        var history = (await _cycles.GetHistoryAsync())
            .Take(3)
            .Select(c => new
            {
                c.Id,
                WeekStartDate = c.WeekStartDate.ToString("yyyy-MM-dd"),
                Status        = c.Status.ToString(),
                memberCount   = c.CycleMembers.Count
            });

        return Ok(new
        {
            activeCycle    = activeCycleSnapshot,
            team           = teamSummary,
            backlog        = backlogSummary,
            recentHistory  = history
        });
    }
}

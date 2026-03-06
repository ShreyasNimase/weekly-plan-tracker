using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ICycleRepository _cycles;
    private readonly ITaskAssignmentRepository _assignments;

    public DashboardService(ICycleRepository cycles, ITaskAssignmentRepository assignments)
    {
        _cycles = cycles;
        _assignments = assignments;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        // Prefer FROZEN, then PLANNING (only one active cycle exists; if it's SETUP we still show it)
        var cycle = await _cycles.GetActiveAsync(cancellationToken);
        if (cycle is null)
            return new DashboardDto();

        var assignments = (await _assignments.GetByCycleIdAsync(cycle.Id, cancellationToken)).ToList();
        var totalCompleted = assignments.Sum(a => a.HoursCompleted);
        var completedTaskCount = assignments.Count(a => string.Equals(a.ProgressStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase));
        var blockedTaskCount = assignments.Count(a => string.Equals(a.ProgressStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase));
        var totalTaskCount = assignments.Count;

        var categoryBreakdown = (cycle.CategoryAllocations ?? []).Select(ca =>
        {
            var catAssignments = assignments.Where(a => a.BacklogItem?.Category == ca.Category).ToList();
            var planned = catAssignments.Sum(a => a.CommittedHours);
            var completed = catAssignments.Sum(a => a.HoursCompleted);
            var pct = ca.BudgetHours > 0 ? Math.Round(completed / ca.BudgetHours * 100, 1) : 0m;
            return new DashboardCategoryBreakdownDto
            {
                Category = ca.Category,
                BudgetHours = ca.BudgetHours,
                PlannedHours = planned,
                CompletedHours = completed,
                Percentage = pct
            };
        }).ToList();

        var memberPlans = cycle.MemberPlans ?? [];
        var memberBreakdown = memberPlans.Select(mp =>
        {
            var mpAssignments = assignments.Where(a => a.MemberPlanId == mp.Id).ToList();
            var planned = mpAssignments.Sum(a => a.CommittedHours);
            var completed = mpAssignments.Sum(a => a.HoursCompleted);
            var completedCount = mpAssignments.Count(a => string.Equals(a.ProgressStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase));
            var isBlocked = mpAssignments.Any(a => string.Equals(a.ProgressStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase));
            var allDone = mpAssignments.Count > 0 && mpAssignments.All(a => string.Equals(a.ProgressStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase));
            return new DashboardMemberBreakdownDto
            {
                MemberId = mp.MemberId,
                MemberName = mp.Member?.Name ?? "",
                PlannedHours = planned,
                CompletedHours = completed,
                IsBlocked = isBlocked,
                AllDone = allDone,
                TaskCount = mpAssignments.Count,
                CompletedTaskCount = completedCount
            };
        }).ToList();

        return new DashboardDto
        {
            CycleId = cycle.Id,
            PlanningDate = cycle.PlanningDate,
            State = cycle.State,
            TeamCapacity = cycle.TeamCapacity,
            TotalCompleted = totalCompleted,
            CompletedTaskCount = completedTaskCount,
            BlockedTaskCount = blockedTaskCount,
            TotalTaskCount = totalTaskCount,
            CategoryBreakdown = categoryBreakdown,
            MemberBreakdown = memberBreakdown
        };
    }
}

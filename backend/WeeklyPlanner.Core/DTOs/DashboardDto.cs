namespace WeeklyPlanner.Core.DTOs;

/// <summary>Response for GET /api/dashboard.</summary>
public class DashboardDto
{
    public Guid? CycleId { get; set; }
    public DateTime? PlanningDate { get; set; }
    public string? State { get; set; }
    public int TeamCapacity { get; set; }
    public decimal TotalCompleted { get; set; }
    public int CompletedTaskCount { get; set; }
    public int BlockedTaskCount { get; set; }
    public int TotalTaskCount { get; set; }
    public List<DashboardCategoryBreakdownDto> CategoryBreakdown { get; set; } = [];
    public List<DashboardMemberBreakdownDto> MemberBreakdown { get; set; } = [];
}

public class DashboardCategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public decimal BudgetHours { get; set; }
    public decimal PlannedHours { get; set; }
    public decimal CompletedHours { get; set; }
    public decimal Percentage { get; set; }
}

public class DashboardMemberBreakdownDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal PlannedHours { get; set; }
    public decimal CompletedHours { get; set; }
    public bool IsBlocked { get; set; }
    public bool AllDone { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
}

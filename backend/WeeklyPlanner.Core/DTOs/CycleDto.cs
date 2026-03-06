namespace WeeklyPlanner.Core.DTOs;

public class CycleDto
{
    public Guid Id { get; set; }
    public DateTime PlanningDate { get; set; }
    public DateTime ExecutionStartDate { get; set; }
    public DateTime ExecutionEndDate { get; set; }
    public string State { get; set; } = string.Empty;
    public int TeamCapacity { get; set; }
    public List<Guid> ParticipatingMemberIds { get; set; } = [];
    public List<CategoryAllocationItemDto> CategoryAllocations { get; set; } = [];
    public List<MemberPlanSummaryDto> MemberPlans { get; set; } = [];
}

public class CategoryAllocationItemDto
{
    public string Category { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public decimal BudgetHours { get; set; }
}

public class MemberPlanSummaryDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public bool IsReady { get; set; }
    public decimal TotalPlannedHours { get; set; }
}

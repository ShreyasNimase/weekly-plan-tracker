namespace WeeklyPlanner.Core.DTOs;

/// <summary>Export payload for GET /api/export and POST /api/import.</summary>
public class ExportPayloadDto
{
    public string AppName { get; set; } = "WeeklyPlanTracker";
    public int DataVersion { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public ExportDataDto Data { get; set; } = new();
}

public class ExportDataDto
{
    public List<ExportTeamMemberDto> TeamMembers { get; set; } = [];
    public List<ExportBacklogItemDto> BacklogEntries { get; set; } = [];
    public List<ExportPlanningCycleDto> PlanningCycles { get; set; } = [];
    public List<ExportCategoryAllocationDto> CategoryAllocations { get; set; } = [];
    public List<ExportCycleMemberDto> CycleMembers { get; set; } = [];
    public List<ExportMemberPlanDto> MemberPlans { get; set; } = [];
    public List<ExportTaskAssignmentDto> TaskAssignments { get; set; } = [];
    public List<ExportProgressUpdateDto> ProgressUpdates { get; set; } = [];
}

public class ExportTeamMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExportBacklogItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? EstimatedEffort { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExportPlanningCycleDto
{
    public Guid Id { get; set; }
    public DateTime PlanningDate { get; set; }
    public DateTime ExecutionStartDate { get; set; }
    public DateTime ExecutionEndDate { get; set; }
    public string State { get; set; } = string.Empty;
    public int TeamCapacity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExportCategoryAllocationDto
{
    public Guid Id { get; set; }
    public Guid CycleId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public decimal BudgetHours { get; set; }
}

public class ExportCycleMemberDto
{
    public Guid Id { get; set; }
    public Guid CycleId { get; set; }
    public Guid MemberId { get; set; }
}

public class ExportMemberPlanDto
{
    public Guid Id { get; set; }
    public Guid CycleId { get; set; }
    public Guid MemberId { get; set; }
    public bool IsReady { get; set; }
    public decimal TotalPlannedHours { get; set; }
}

public class ExportTaskAssignmentDto
{
    public Guid Id { get; set; }
    public Guid MemberPlanId { get; set; }
    public Guid BacklogItemId { get; set; }
    public decimal CommittedHours { get; set; }
    public string ProgressStatus { get; set; } = string.Empty;
    public decimal HoursCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExportProgressUpdateDto
{
    public Guid Id { get; set; }
    public Guid TaskAssignmentId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal PreviousHoursCompleted { get; set; }
    public decimal NewHoursCompleted { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid UpdatedBy { get; set; }
}

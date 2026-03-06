namespace WeeklyPlanner.Core.DTOs;

/// <summary>Response for GET /api/cycles/{id}/progress.</summary>
public class CycleProgressDto
{
    public List<MemberProgressItemDto> Members { get; set; } = [];
}

public class MemberProgressItemDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public decimal TotalPlanned { get; set; }
    public List<AssignmentProgressItemDto> Assignments { get; set; } = [];
}

public class AssignmentProgressItemDto
{
    public Guid Id { get; set; }
    public Guid BacklogItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal CommittedHours { get; set; }
    public decimal HoursCompleted { get; set; }
    public string ProgressStatus { get; set; } = string.Empty;
}

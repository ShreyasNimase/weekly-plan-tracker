namespace WeeklyPlanner.Core.DTOs;

/// <summary>Response for GET /api/cycles/{id}/members/{memberId}/progress (single member with full history).</summary>
public class MemberProgressDetailDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public decimal TotalPlanned { get; set; }
    public List<AssignmentWithHistoryDto> Assignments { get; set; } = [];
}

public class AssignmentWithHistoryDto
{
    public Guid Id { get; set; }
    public Guid BacklogItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal CommittedHours { get; set; }
    public decimal HoursCompleted { get; set; }
    public string ProgressStatus { get; set; } = string.Empty;
    public List<ProgressUpdateItemDto> ProgressUpdates { get; set; } = [];
}

public class ProgressUpdateItemDto
{
    public Guid Id { get; set; }
    public decimal PreviousHoursCompleted { get; set; }
    public decimal NewHoursCompleted { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid UpdatedBy { get; set; }
}

namespace WeeklyPlanner.Core.DTOs;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid MemberPlanId { get; set; }
    public Guid BacklogItemId { get; set; }
    public string BacklogItemTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal CommittedHours { get; set; }
    public string ProgressStatus { get; set; } = string.Empty;
    public decimal HoursCompleted { get; set; }
}

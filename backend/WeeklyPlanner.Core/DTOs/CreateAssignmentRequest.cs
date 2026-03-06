namespace WeeklyPlanner.Core.DTOs;

public class CreateAssignmentRequest
{
    public Guid MemberPlanId { get; set; }
    public Guid BacklogItemId { get; set; }
    public decimal CommittedHours { get; set; }
}

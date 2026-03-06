using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class ClaimBacklogItemDto
{
    [Required]
    public Guid MemberPlanId { get; set; }

    [Required]
    public Guid BacklogItemId { get; set; }

    [Required]
    [Range(0.5, 30, ErrorMessage = "Committed hours must be between 0.5 and 30.")]
    public decimal CommittedHours { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class ClaimBacklogItemDto
{
    [Required]
    public Guid CycleMemberId { get; set; }

    [Required]
    public Guid BacklogItemId { get; set; }

    [Required]
    [Range(0.5, 30, ErrorMessage = "Planned hours must be between 0.5 and 30.")]
    public decimal PlannedHours { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class UpdateAssignmentDto
{
    [Required]
    [Range(0.5, 30, ErrorMessage = "Planned hours must be between 0.5 and 30.")]
    public decimal PlannedHours { get; set; }
}

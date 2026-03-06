using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class StartCycleDto
{
    [Required(ErrorMessage = "PlanningDate is required (must be a Tuesday).")]
    public DateTime PlanningDate { get; set; }
}

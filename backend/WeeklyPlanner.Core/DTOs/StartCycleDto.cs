using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class StartCycleDto
{
    [Required(ErrorMessage = "WeekStartDate is required.")]
    public DateTime WeekStartDate { get; set; }
}

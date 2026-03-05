using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class UpdateTeamMemberDto
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
}

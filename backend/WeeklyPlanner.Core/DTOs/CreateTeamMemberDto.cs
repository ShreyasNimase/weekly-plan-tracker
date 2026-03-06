using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class CreateTeamMemberDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsLead { get; set; }
}

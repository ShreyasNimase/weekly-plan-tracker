using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// Represents a team member who can participate in planning cycles.
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsLead { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace WeeklyPlanner.Core.DTOs;

/// <summary>Response DTO for a team member.</summary>
public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

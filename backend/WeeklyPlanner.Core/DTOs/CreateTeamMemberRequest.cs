namespace WeeklyPlanner.Core.DTOs;

/// <summary>Request body for POST /api/team-members.</summary>
public class CreateTeamMemberRequest
{
    public string Name { get; set; } = string.Empty;
}

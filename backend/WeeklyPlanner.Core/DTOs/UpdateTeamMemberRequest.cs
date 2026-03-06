namespace WeeklyPlanner.Core.DTOs;

/// <summary>Request body for PUT /api/team-members/{id}.</summary>
public class UpdateTeamMemberRequest
{
    public string Name { get; set; } = string.Empty;
}

namespace WeeklyPlanner.Core.Entities;

public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    public bool IsActive { get; set; } = true;
}

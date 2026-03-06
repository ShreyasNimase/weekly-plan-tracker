namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// Join table: cycle participants (which members are in a cycle).
/// </summary>
public class CycleMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CycleId { get; set; }
    public PlanningCycle Cycle { get; set; } = null!;

    public Guid MemberId { get; set; }
    public TeamMember Member { get; set; } = null!;
}

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// A member's plan for a cycle (readiness and total planned hours).
/// </summary>
public class MemberPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CycleId { get; set; }
    public PlanningCycle Cycle { get; set; } = null!;

    public Guid MemberId { get; set; }
    public TeamMember Member { get; set; } = null!;

    public bool IsReady { get; set; }

    public decimal TotalPlannedHours { get; set; }

    public List<TaskAssignment> TaskAssignments { get; set; } = [];
}

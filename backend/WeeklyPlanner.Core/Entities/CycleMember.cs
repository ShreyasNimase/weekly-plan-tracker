namespace WeeklyPlanner.Core.Entities;

public class CycleMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CycleId { get; set; }
    public PlanningCycle Cycle { get; set; } = null!;

    public Guid TeamMemberId { get; set; }
    public TeamMember TeamMember { get; set; } = null!;

    /// <summary>Each member is allocated exactly 30 hours per cycle.</summary>
    public decimal AllocatedHours { get; set; } = 30m;

    /// <summary>Set to true when member marks their plan complete via PUT /api/member-plans/{id}/ready.</summary>
    public bool IsReady { get; set; } = false;

    public List<TaskAssignment> TaskAssignments { get; set; } = [];
}

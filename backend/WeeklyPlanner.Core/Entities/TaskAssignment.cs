using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.Entities;

public class TaskAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CycleMemberId { get; set; }
    public CycleMember CycleMember { get; set; } = null!;

    public Guid BacklogItemId { get; set; }
    public BacklogItem BacklogItem { get; set; } = null!;

    /// <summary>
    /// Hours planned for this task. Must be in 0.5-hour increments.
    /// Enforced by the controller on create and update.
    /// </summary>
    public decimal PlannedHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

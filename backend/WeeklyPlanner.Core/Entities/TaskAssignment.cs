using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// Assignment of a backlog item to a member plan. ProgressStatus: NOT_STARTED, IN_PROGRESS, COMPLETED, BLOCKED.
/// </summary>
public class TaskAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MemberPlanId { get; set; }
    public MemberPlan MemberPlan { get; set; } = null!;

    public Guid BacklogItemId { get; set; }
    public BacklogItem BacklogItem { get; set; } = null!;

    /// <summary>Committed hours; > 0, 0.5 steps.</summary>
    public decimal CommittedHours { get; set; }

    [Required]
    public string ProgressStatus { get; set; } = string.Empty;

    /// <summary>Hours completed so far; ≥ 0.</summary>
    public decimal HoursCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ProgressUpdate> ProgressUpdates { get; set; } = [];
}

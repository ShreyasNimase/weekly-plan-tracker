using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// Audit/history record for a task assignment progress change.
/// </summary>
public class ProgressUpdate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskAssignmentId { get; set; }
    public TaskAssignment TaskAssignment { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public decimal PreviousHoursCompleted { get; set; }
    public decimal NewHoursCompleted { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }

    public Guid UpdatedBy { get; set; }
    public TeamMember UpdatedByMember { get; set; } = null!;
}

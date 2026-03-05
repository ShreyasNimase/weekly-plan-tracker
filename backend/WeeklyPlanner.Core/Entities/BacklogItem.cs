using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.Entities;

public class BacklogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public BacklogCategory Category { get; set; } = BacklogCategory.Feature;

    public BacklogStatus Status { get; set; } = BacklogStatus.Active;

    public BacklogPriority Priority { get; set; } = BacklogPriority.Medium;

    /// <summary>
    /// Estimated effort in hours. Must be in 0.5-hour increments.
    /// Enforcement happens at assignment time (Planning phase), not here.
    /// </summary>
    public decimal? EstimatedHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

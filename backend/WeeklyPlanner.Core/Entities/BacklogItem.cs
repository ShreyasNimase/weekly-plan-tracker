using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// A backlog item that can be planned and assigned in cycles.
/// Category: CLIENT_FOCUSED, TECH_DEBT, R_AND_D.
/// Status: AVAILABLE, IN_PLAN, COMPLETED, ARCHIVED.
/// </summary>
public class BacklogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>Estimated effort in hours; 0.5 steps, max 999.5.</summary>
    public decimal? EstimatedEffort { get; set; }

    public Guid CreatedBy { get; set; }
    public TeamMember CreatedByMember { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

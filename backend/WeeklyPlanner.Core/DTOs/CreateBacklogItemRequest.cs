namespace WeeklyPlanner.Core.DTOs;

/// <summary>Request body for POST /api/backlog.</summary>
public class CreateBacklogItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal? EstimatedEffort { get; set; }
    /// <summary>Team member who created the item (required for FK).</summary>
    public Guid? CreatedBy { get; set; }
}

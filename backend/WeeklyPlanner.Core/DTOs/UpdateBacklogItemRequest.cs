namespace WeeklyPlanner.Core.DTOs;

/// <summary>Request body for PUT /api/backlog/{id}. Category is not updatable.</summary>
public class UpdateBacklogItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? EstimatedEffort { get; set; }
}

namespace WeeklyPlanner.Core.DTOs;

/// <summary>Response for GET /api/cycles/{id}/category-progress.</summary>
public class CategoryProgressDto
{
    public string Category { get; set; } = string.Empty;
    public decimal BudgetHours { get; set; }
    public decimal TotalCommitted { get; set; }
    public decimal TotalCompleted { get; set; }
    public int CompletedTaskCount { get; set; }
    public int TotalTaskCount { get; set; }
}

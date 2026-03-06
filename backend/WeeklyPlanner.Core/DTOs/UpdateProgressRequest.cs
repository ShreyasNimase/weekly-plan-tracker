namespace WeeklyPlanner.Core.DTOs;

/// <summary>Request body for PUT /api/assignments/{id}/progress.</summary>
public class UpdateProgressRequest
{
    public string ProgressStatus { get; set; } = string.Empty;
    public decimal HoursCompleted { get; set; }
    public string? Note { get; set; }
}

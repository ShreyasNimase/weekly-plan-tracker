using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class CreateBacklogItemDto
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters.")]
    public string? Description { get; set; }

    /// <summary>CLIENT_FOCUSED, TECH_DEBT, or R_AND_D.</summary>
    [Required]
    public string Category { get; set; } = "CLIENT_FOCUSED";

    [Range(0.5, 999.5, ErrorMessage = "Estimated effort must be between 0.5 and 999.5 (0.5 steps).")]
    public decimal? EstimatedEffort { get; set; }

    [Required]
    public Guid CreatedBy { get; set; }
}

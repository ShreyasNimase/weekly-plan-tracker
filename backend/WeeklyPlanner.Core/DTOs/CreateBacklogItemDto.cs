using System.ComponentModel.DataAnnotations;
using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.DTOs;

public class CreateBacklogItemDto
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    public BacklogCategory Category { get; set; } = BacklogCategory.Feature;

    public BacklogPriority Priority { get; set; } = BacklogPriority.Medium;

    [Range(0.5, 200, ErrorMessage = "Estimated hours must be between 0.5 and 200.")]
    public decimal? EstimatedHours { get; set; }
}

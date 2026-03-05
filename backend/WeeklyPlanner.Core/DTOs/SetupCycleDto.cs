using System.ComponentModel.DataAnnotations;
using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.DTOs;

public class SetupCycleDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one team member is required.")]
    public List<Guid> MemberIds { get; set; } = [];

    [Required]
    [MinLength(1, ErrorMessage = "At least one category budget is required.")]
    public List<CategoryBudgetDto> CategoryBudgets { get; set; } = [];
}

public class CategoryBudgetDto
{
    public BacklogCategory Category { get; set; }

    [Range(0.1, 100, ErrorMessage = "Percentage must be between 0.1 and 100.")]
    public decimal Percentage { get; set; }
}

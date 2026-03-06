using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.DTOs;

public class SetupCycleDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one team member is required.")]
    public List<Guid> MemberIds { get; set; } = [];

    [Required]
    [MinLength(1, ErrorMessage = "At least one category allocation is required.")]
    public List<CategoryAllocationDto> CategoryAllocations { get; set; } = [];
}

public class CategoryAllocationDto
{
    /// <summary>CLIENT_FOCUSED, TECH_DEBT, or R_AND_D.</summary>
    public string Category { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100.")]
    public int Percentage { get; set; }
}

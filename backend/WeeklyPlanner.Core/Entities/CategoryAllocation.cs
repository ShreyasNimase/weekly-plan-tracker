using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// Category allocation for a cycle. Category: CLIENT_FOCUSED, TECH_DEBT, R_AND_D.
/// Percentage 0–100; BudgetHours computed from cycle capacity.
/// </summary>
public class CategoryAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CycleId { get; set; }
    public PlanningCycle Cycle { get; set; } = null!;

    [Required]
    public string Category { get; set; } = string.Empty;

    /// <summary>0–100.</summary>
    public int Percentage { get; set; }

    public decimal BudgetHours { get; set; }
}

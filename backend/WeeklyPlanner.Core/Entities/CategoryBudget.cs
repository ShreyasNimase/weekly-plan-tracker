using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.Entities;

public class CategoryBudget
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CycleId { get; set; }
    public PlanningCycle Cycle { get; set; } = null!;

    public BacklogCategory Category { get; set; }

    /// <summary>Percentage of total hours for this category, e.g. 40.0 = 40%.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Computed = TotalMembers * 30 * Percentage / 100.</summary>
    public decimal HoursBudget { get; set; }
}

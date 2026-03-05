using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.Entities;

public class PlanningCycle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Must be a Tuesday (enforced by controller).</summary>
    public DateTime WeekStartDate { get; set; }

    public CycleStatus Status { get; set; } = CycleStatus.Setup;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<CycleMember> CycleMembers { get; set; } = [];
    public List<CategoryBudget> CategoryBudgets { get; set; } = [];
}

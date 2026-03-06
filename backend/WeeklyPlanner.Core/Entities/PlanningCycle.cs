using System.ComponentModel.DataAnnotations;

namespace WeeklyPlanner.Core.Entities;

/// <summary>
/// A planning cycle (week). PlanningDate must be Tuesday.
/// State: SETUP, PLANNING, FROZEN, COMPLETED.
/// </summary>
public class PlanningCycle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Must be a Tuesday (enforced by application).</summary>
    public DateTime PlanningDate { get; set; }

    /// <summary>PlanningDate + 1 day.</summary>
    public DateTime ExecutionStartDate { get; set; }

    /// <summary>PlanningDate + 6 days.</summary>
    public DateTime ExecutionEndDate { get; set; }

    [Required]
    public string State { get; set; } = string.Empty;

    /// <summary>Participating members × 30.</summary>
    public int TeamCapacity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CycleMember> CycleMembers { get; set; } = [];
    public List<CategoryAllocation> CategoryAllocations { get; set; } = [];
    public List<MemberPlan> MemberPlans { get; set; } = [];
}

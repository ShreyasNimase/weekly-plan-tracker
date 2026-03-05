using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

public interface ICycleRepository
{
    Task<PlanningCycle> AddAsync(PlanningCycle cycle);
    Task<PlanningCycle?> GetActiveAsync();
    Task<PlanningCycle?> GetByIdAsync(Guid id);
    Task<PlanningCycle> UpdateAsync(PlanningCycle cycle);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<PlanningCycle>> GetHistoryAsync();
    Task<bool> HasActiveCycleAsync();

    /// <summary>
    /// Replaces all CycleMembers and CategoryBudgets on the cycle.
    /// Uses explicit RemoveRange to avoid EF tracking conflicts.
    /// </summary>
    Task SetupMembersAndBudgetsAsync(PlanningCycle cycle, List<CycleMember> members, List<CategoryBudget> budgets);
}

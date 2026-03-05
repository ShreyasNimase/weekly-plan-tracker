using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

public class CycleRepository : ICycleRepository
{
    private readonly AppDbContext _context;

    public CycleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlanningCycle> AddAsync(PlanningCycle cycle)
    {
        _context.PlanningCycles.Add(cycle);
        await _context.SaveChangesAsync();
        return cycle;
    }

    public async Task<PlanningCycle?> GetActiveAsync()
    {
        return await _context.PlanningCycles
            .Include(c => c.CycleMembers)
                .ThenInclude(cm => cm.TeamMember)
            .Include(c => c.CategoryBudgets)
            .FirstOrDefaultAsync(c =>
                c.Status != CycleStatus.Completed &&
                c.Status != CycleStatus.Cancelled);
    }

    public async Task<PlanningCycle?> GetByIdAsync(Guid id)
    {
        return await _context.PlanningCycles
            .Include(c => c.CycleMembers)
                .ThenInclude(cm => cm.TeamMember)
            .Include(c => c.CategoryBudgets)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<PlanningCycle> UpdateAsync(PlanningCycle cycle)
    {
        // If already tracked (loaded via GetByIdAsync with Include), EF detects
        // all changes automatically — calling .Update() would re-mark as Modified
        // and cause DbUpdateConcurrencyException when children were also mutated.
        if (_context.Entry(cycle).State == EntityState.Detached)
        {
            _context.PlanningCycles.Update(cycle);
        }

        await _context.SaveChangesAsync();
        return cycle;
    }

    public async Task DeleteAsync(Guid id)
    {
        var cycle = await _context.PlanningCycles.FindAsync(id);
        if (cycle is not null)
        {
            cycle.Status = CycleStatus.Cancelled;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetupMembersAndBudgetsAsync(
        PlanningCycle cycle,
        List<CycleMember> members,
        List<CategoryBudget> budgets)
    {
        // Explicitly remove existing children so EF issues DELETE statements
        _context.CycleMembers.RemoveRange(
            _context.CycleMembers.Where(cm => cm.CycleId == cycle.Id));
        _context.CategoryBudgets.RemoveRange(
            _context.CategoryBudgets.Where(cb => cb.CycleId == cycle.Id));

        // Assign the CycleId to new children
        foreach (var m in members)  m.CycleId = cycle.Id;
        foreach (var b in budgets)  b.CycleId = cycle.Id;

        _context.CycleMembers.AddRange(members);
        _context.CategoryBudgets.AddRange(budgets);

        await _context.SaveChangesAsync();

        // Refresh navigation properties in memory
        cycle.CycleMembers  = members;
        cycle.CategoryBudgets = budgets;
    }

    public async Task<IEnumerable<PlanningCycle>> GetHistoryAsync()
    {
        return await _context.PlanningCycles
            .Include(c => c.CycleMembers)
                .ThenInclude(cm => cm.TeamMember)
            .Include(c => c.CategoryBudgets)
            .Where(c => c.Status == CycleStatus.Completed || c.Status == CycleStatus.Cancelled)
            .OrderByDescending(c => c.WeekStartDate)
            .ToListAsync();
    }

    public async Task<bool> HasActiveCycleAsync()
    {
        return await _context.PlanningCycles
            .AnyAsync(c =>
                c.Status != CycleStatus.Completed &&
                c.Status != CycleStatus.Cancelled);
    }
}

using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

/// <summary>
/// Repository for planning cycles. Call <see cref="IUnitOfWork.SaveChangesAsync"/> after add/update/delete/setup.
/// </summary>
public class CycleRepository : ICycleRepository
{
    private readonly AppDbContext _context;

    public CycleRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PlanningCycle> AddAsync(PlanningCycle cycle, CancellationToken cancellationToken = default)
    {
        _context.PlanningCycles.Add(cycle);
        await _context.SaveChangesAsync(cancellationToken);
        return cycle;
    }

    /// <inheritdoc />
    public async Task<PlanningCycle?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PlanningCycles
            .Include(c => c.CycleMembers!)
            .ThenInclude(cm => cm.Member)
            .Include(c => c.CategoryAllocations)
            .Include(c => c.MemberPlans!)
            .ThenInclude(mp => mp.Member)
            .Include(c => c.MemberPlans!)
            .ThenInclude(mp => mp.TaskAssignments!)
            .FirstOrDefaultAsync(c => c.State == "SETUP" || c.State == "PLANNING" || c.State == "FROZEN", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanningCycle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PlanningCycles
            .Include(c => c.CycleMembers!)
            .ThenInclude(cm => cm.Member)
            .Include(c => c.CategoryAllocations)
            .Include(c => c.MemberPlans!)
            .ThenInclude(mp => mp.Member)
            .Include(c => c.MemberPlans!)
            .ThenInclude(mp => mp.TaskAssignments!)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanningCycle> UpdateAsync(PlanningCycle cycle, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(cycle).State == EntityState.Detached)
            _context.PlanningCycles.Update(cycle);
        await _context.SaveChangesAsync(cancellationToken);
        return cycle;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cycle = await _context.PlanningCycles.FindAsync([id], cancellationToken);
        if (cycle is not null)
        {
            _context.PlanningCycles.Remove(cycle);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PlanningCycle>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PlanningCycles
            .Include(c => c.CycleMembers!)
            .ThenInclude(cm => cm.Member)
            .Include(c => c.CategoryAllocations)
            .Where(c => c.State == "FROZEN" || c.State == "COMPLETED")
            .OrderByDescending(c => c.PlanningDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveCycleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PlanningCycles
            .AnyAsync(c => c.State == "SETUP" || c.State == "PLANNING" || c.State == "FROZEN", cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetupMembersAndAllocationsAsync(PlanningCycle cycle, List<CycleMember> members, List<CategoryAllocation> allocations, CancellationToken cancellationToken = default)
    {
        _context.CycleMembers.RemoveRange(
            _context.CycleMembers.Where(cm => cm.CycleId == cycle.Id));
        _context.CategoryAllocations.RemoveRange(
            _context.CategoryAllocations.Where(ca => ca.CycleId == cycle.Id));

        foreach (var m in members) m.CycleId = cycle.Id;
        foreach (var a in allocations) a.CycleId = cycle.Id;

        _context.CycleMembers.AddRange(members);
        _context.CategoryAllocations.AddRange(allocations);

        await _context.SaveChangesAsync(cancellationToken);

        cycle.CycleMembers = members;
        cycle.CategoryAllocations = allocations;
    }
}

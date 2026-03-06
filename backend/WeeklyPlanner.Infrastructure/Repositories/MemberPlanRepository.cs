using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

/// <summary>
/// Repository for member plans. Call <see cref="IUnitOfWork.SaveChangesAsync"/> after add/update/delete.
/// </summary>
public class MemberPlanRepository : IMemberPlanRepository
{
    private readonly AppDbContext _context;

    public MemberPlanRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<MemberPlan> AddAsync(MemberPlan memberPlan, CancellationToken cancellationToken = default)
    {
        _context.MemberPlans.Add(memberPlan);
        await _context.SaveChangesAsync(cancellationToken);
        return memberPlan;
    }

    /// <inheritdoc />
    public async Task<MemberPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MemberPlans
            .Include(mp => mp.Cycle)
            .ThenInclude(c => c!.CategoryAllocations)
            .Include(mp => mp.Member)
            .Include(mp => mp.TaskAssignments!)
            .ThenInclude(ta => ta.BacklogItem)
            .FirstOrDefaultAsync(mp => mp.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MemberPlan?> GetByCycleAndMemberAsync(Guid cycleId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.MemberPlans
            .Include(mp => mp.Member)
            .Include(mp => mp.TaskAssignments!)
            .ThenInclude(ta => ta.BacklogItem)
            .FirstOrDefaultAsync(mp => mp.CycleId == cycleId && mp.MemberId == memberId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MemberPlan>> GetByCycleIdAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        return await _context.MemberPlans
            .Include(mp => mp.Member)
            .Include(mp => mp.TaskAssignments!)
            .ThenInclude(ta => ta.BacklogItem)
            .Where(mp => mp.CycleId == cycleId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MemberPlan> UpdateAsync(MemberPlan memberPlan, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(memberPlan).State == EntityState.Detached)
            _context.MemberPlans.Update(memberPlan);
        await _context.SaveChangesAsync(cancellationToken);
        return memberPlan;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _context.MemberPlans.FindAsync([id], cancellationToken);
        if (plan is not null)
        {
            _context.MemberPlans.Remove(plan);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteByCycleIdAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var plans = await _context.MemberPlans.Where(mp => mp.CycleId == cycleId).ToListAsync(cancellationToken);
        _context.MemberPlans.RemoveRange(plans);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

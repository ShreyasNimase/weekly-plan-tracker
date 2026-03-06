using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Helpers;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.Infrastructure.Services;

public class CycleService : ICycleService
{
    private readonly ICycleRepository _cycles;
    private readonly ITeamMemberRepository _members;
    private readonly IMemberPlanRepository _memberPlans;
    private readonly IBacklogRepository _backlog;
    private readonly ITaskAssignmentRepository _assignments;

    public CycleService(
        ICycleRepository cycles,
        ITeamMemberRepository members,
        IMemberPlanRepository memberPlans,
        IBacklogRepository backlog,
        ITaskAssignmentRepository assignments)
    {
        _cycles = cycles;
        _members = members;
        _memberPlans = memberPlans;
        _backlog = backlog;
        _assignments = assignments;
    }

    public async Task<(CycleDto? Result, string? Error)> StartAsync(CancellationToken cancellationToken = default)
    {
        if (await _cycles.HasActiveCycleAsync(cancellationToken))
            return (null, "There is already a week being planned.");

        var today = DateTime.UtcNow.Date;
        var nextTuesday = GetNextTuesday(today);

        var cycle = new PlanningCycle
        {
            PlanningDate = nextTuesday,
            ExecutionStartDate = nextTuesday.AddDays(1),
            ExecutionEndDate = nextTuesday.AddDays(6),
            State = "SETUP",
            TeamCapacity = 0,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _cycles.AddAsync(cycle, cancellationToken);
        return (ToDto(created), null);
    }

    public async Task<(CycleDto? Result, string? Error)> SetupAsync(Guid cycleId, SetupCycleRequest request, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null)
            return (null, "Cycle not found.");
        if (cycle.State != "SETUP")
            return (null, $"Cannot configure a cycle in '{cycle.State}' state. Only SETUP cycles can be configured.");

        if (request.PlanningDate.DayOfWeek != DayOfWeek.Tuesday)
            return (null, "Planning date must be a Tuesday.");

        var memberIds = request.MemberIds?.Distinct().ToList() ?? new List<Guid>();
        if (memberIds.Count == 0)
            return (null, "At least one team member is required.");

        var invalidMembers = new List<Guid>();
        foreach (var id in memberIds)
        {
            var m = await _members.GetByIdAsync(id, cancellationToken);
            if (m is null || !m.IsActive)
                invalidMembers.Add(id);
        }
        if (invalidMembers.Count > 0)
            return (null, "One or more member IDs are invalid or inactive.");

        var allocs = request.CategoryAllocations?.ToList() ?? new List<CategoryAllocationInputDto>();
        if (allocs.Count != 3)
            return (null, "Category allocations must contain exactly 3 entries (one per category).");
        var totalPct = allocs.Sum(a => a.Percentage);
        if (totalPct != 100)
            return (null, $"Category percentages must total 100. Current total: {totalPct}.");

        int memberCount = memberIds.Count;
        decimal cap = memberCount * 30m;
        var allocationInputs = allocs.Select(a => (a.Category, a.Percentage)).ToList();
        var budgetHours = BudgetHoursHelper.CalculateBudgetHours(memberCount, allocationInputs);

        cycle.PlanningDate = request.PlanningDate.Date;
        cycle.ExecutionStartDate = cycle.PlanningDate.AddDays(1);
        cycle.ExecutionEndDate = cycle.PlanningDate.AddDays(6);
        cycle.TeamCapacity = memberCount * 30;

        await _memberPlans.DeleteByCycleIdAsync(cycleId, cancellationToken);
        await _cycles.SetupMembersAndAllocationsAsync(cycle,
            memberIds.Select(mid => new CycleMember { MemberId = mid }).ToList(),
            allocs.Select(a => new CategoryAllocation
            {
                Category = a.Category,
                Percentage = a.Percentage,
                BudgetHours = budgetHours.TryGetValue(a.Category, out var bh) ? bh : 0
            }).ToList(),
            cancellationToken);

        foreach (var mid in memberIds)
        {
            await _memberPlans.AddAsync(new MemberPlan
            {
                CycleId = cycleId,
                MemberId = mid,
                IsReady = false,
                TotalPlannedHours = 0
            }, cancellationToken);
        }

        var updated = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        return (updated != null ? ToDto(updated) : null, null);
    }

    public async Task<(CycleDto? Result, string? Error)> OpenAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null)
            return (null, "Cycle not found.");
        if (cycle.State != "SETUP")
            return (null, $"Cannot open planning from '{cycle.State}' state. Cycle must be in SETUP.");
        if (!cycle.CycleMembers!.Any())
            return (null, "Cannot open planning without any members. Run setup first.");
        if (!cycle.CategoryAllocations!.Any())
            return (null, "Cannot open planning without category allocations. Run setup first.");

        cycle.State = "PLANNING";
        await _cycles.UpdateAsync(cycle, cancellationToken);
        return (ToDto(cycle), null);
    }

    public async Task<CycleDto?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetActiveAsync(cancellationToken);
        return cycle is null ? null : ToDto(cycle);
    }

    public async Task<(CycleDto? Result, List<string>? Errors)> FreezeAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null)
            return (null, new List<string> { "Cycle not found." });
        if (cycle.State != "PLANNING")
            return (null, new List<string> { $"Cannot freeze from '{cycle.State}' state. Cycle must be in PLANNING." });

        var errors = new List<string>();
        var memberPlans = cycle.MemberPlans ?? (await _memberPlans.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();

        foreach (var mp in memberPlans)
        {
            var total = mp.TotalPlannedHours;
            if (total != 30m)
                errors.Add($"Member plan {mp.Member?.Name ?? mp.Id.ToString()}: TotalPlannedHours is {total}, must be 30.");
        }

        var allocations = cycle.CategoryAllocations ?? new List<CategoryAllocation>();
        var cycleAssignments = (await _assignments.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();
        foreach (var ca in allocations)
        {
            var used = cycleAssignments.Where(a => a.BacklogItem?.Category == ca.Category).Sum(a => a.CommittedHours);
            if (used != ca.BudgetHours)
                errors.Add($"Category {ca.Category}: committed hours total {used}, must equal budget {ca.BudgetHours}.");
        }

        if (errors.Count > 0)
            return (null, errors);

        cycle.State = "FROZEN";
        await _cycles.UpdateAsync(cycle, cancellationToken);
        return (ToDto(cycle), null);
    }

    public async Task<(CycleDto? Result, string? Error)> CompleteAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null)
            return (null, "Cycle not found.");
        if (cycle.State != "FROZEN")
            return (null, $"Cannot complete from '{cycle.State}' state. Cycle must be FROZEN first.");

        cycle.State = "COMPLETED";
        await _cycles.UpdateAsync(cycle, cancellationToken);

        var cycleAssignments = (await _assignments.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();
        var byBacklogItem = cycleAssignments.GroupBy(a => a.BacklogItemId).ToList();
        foreach (var grp in byBacklogItem)
        {
            var item = await _backlog.GetByIdAsync(grp.Key, cancellationToken);
            if (item is null) continue;
            bool allCompleted = grp.All(a => a.ProgressStatus == "COMPLETED");
            item.Status = allCompleted ? "COMPLETED" : "AVAILABLE";
            await _backlog.UpdateAsync(item, cancellationToken);
        }

        var updated = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        return (updated != null ? ToDto(updated) : null, null);
    }

    public async Task<string?> DeleteAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null)
            return "Cycle not found.";
        if (cycle.State != "SETUP" && cycle.State != "PLANNING")
            return "Cannot delete a cycle in FROZEN or COMPLETED state.";

        var cycleAssignments = (await _assignments.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();
        var backlogItemIds = cycleAssignments.Select(a => a.BacklogItemId).Distinct().ToList();

        await _cycles.DeleteAsync(cycleId, cancellationToken);

        foreach (var bid in backlogItemIds)
        {
            var item = await _backlog.GetByIdAsync(bid, cancellationToken);
            if (item is null || item.Status != "IN_PLAN") continue;
            if (!await _assignments.IsBacklogItemClaimedInActiveCycleAsync(bid, null, cancellationToken))
            {
                item.Status = "AVAILABLE";
                await _backlog.UpdateAsync(item, cancellationToken);
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<CycleDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var list = await _cycles.GetHistoryAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    private static DateTime GetNextTuesday(DateTime from)
    {
        int diff = ((int)DayOfWeek.Tuesday - (int)from.DayOfWeek + 7) % 7;
        if (diff == 0)
            diff = 7;
        return from.AddDays(diff);
    }

    private static CycleDto ToDto(PlanningCycle c)
    {
        return new CycleDto
        {
            Id = c.Id,
            PlanningDate = c.PlanningDate,
            ExecutionStartDate = c.ExecutionStartDate,
            ExecutionEndDate = c.ExecutionEndDate,
            State = c.State,
            TeamCapacity = c.TeamCapacity,
            ParticipatingMemberIds = c.CycleMembers?.Select(cm => cm.MemberId).ToList() ?? new List<Guid>(),
            CategoryAllocations = c.CategoryAllocations?.Select(ca => new CategoryAllocationItemDto
            {
                Category = ca.Category,
                Percentage = ca.Percentage,
                BudgetHours = ca.BudgetHours
            }).ToList() ?? new List<CategoryAllocationItemDto>(),
            MemberPlans = c.MemberPlans?.Select(mp => new MemberPlanSummaryDto
            {
                Id = mp.Id,
                MemberId = mp.MemberId,
                IsReady = mp.IsReady,
                TotalPlannedHours = mp.TotalPlannedHours
            }).ToList() ?? new List<MemberPlanSummaryDto>()
        };
    }
}

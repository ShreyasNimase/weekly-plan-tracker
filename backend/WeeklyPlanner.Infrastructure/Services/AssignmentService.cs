using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.Infrastructure.Services;

public class AssignmentService : IAssignmentService
{
    private readonly ITaskAssignmentRepository _assignments;
    private readonly IMemberPlanRepository _memberPlans;
    private readonly IBacklogRepository _backlog;

    public AssignmentService(
        ITaskAssignmentRepository assignments,
        IMemberPlanRepository memberPlans,
        IBacklogRepository backlog)
    {
        _assignments = assignments;
        _memberPlans = memberPlans;
        _backlog = backlog;
    }

    public async Task<(AssignmentDto? Result, string? Error)> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CommittedHours <= 0 || request.CommittedHours % 0.5m != 0)
            return (null, "Committed hours must be greater than 0 and in 0.5 increments.");

        var memberPlan = await _memberPlans.GetByIdAsync(request.MemberPlanId, cancellationToken);
        if (memberPlan is null)
            return (null, "Member plan not found.");
        if (memberPlan.Cycle?.State != "PLANNING")
            return (null, "Cycle must be in PLANNING state to add assignments.");

        var backlogItem = await _backlog.GetByIdAsync(request.BacklogItemId, cancellationToken);
        if (backlogItem is null)
            return (null, "Backlog item not found.");
        if (backlogItem.Status != "AVAILABLE" && backlogItem.Status != "IN_PLAN")
            return (null, "Backlog item must be AVAILABLE or IN_PLAN to assign.");

        var usedByMember = await _assignments.GetTotalHoursForMemberPlanAsync(request.MemberPlanId, cancellationToken);
        var remaining = 30m - usedByMember;
        if (request.CommittedHours > remaining)
            return (null, $"You only have {remaining} hours left.");

        var categoryUsed = await _assignments.GetCategoryHoursUsedAsync(memberPlan.CycleId, backlogItem.Category, cancellationToken);
        var allocation = memberPlan.Cycle.CategoryAllocations?.FirstOrDefault(ca => ca.Category == backlogItem.Category);
        if (allocation is not null)
        {
            var categoryRemaining = allocation.BudgetHours - categoryUsed;
            if (request.CommittedHours > categoryRemaining)
                return (null, $"The {backlogItem.Category} budget only has {categoryRemaining} hours left.");
        }

        var assignment = new TaskAssignment
        {
            MemberPlanId = request.MemberPlanId,
            BacklogItemId = request.BacklogItemId,
            CommittedHours = request.CommittedHours,
            ProgressStatus = "NOT_STARTED",
            HoursCompleted = 0,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _assignments.AddAsync(assignment, cancellationToken);

        if (backlogItem.Status == "AVAILABLE")
        {
            backlogItem.Status = "IN_PLAN";
            await _backlog.UpdateAsync(backlogItem, cancellationToken);
        }

        memberPlan.TotalPlannedHours += request.CommittedHours;
        memberPlan.IsReady = false;
        await _memberPlans.UpdateAsync(memberPlan, cancellationToken);

        return (ToDto(created), null);
    }

    public async Task<(AssignmentDto? Result, string? Error)> UpdateAsync(Guid assignmentId, UpdateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CommittedHours <= 0 || request.CommittedHours % 0.5m != 0)
            return (null, "Committed hours must be greater than 0 and in 0.5 increments.");

        var assignment = await _assignments.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
            return (null, "Assignment not found.");
        if (assignment.MemberPlan?.Cycle?.State != "PLANNING")
            return (null, "Cycle must be in PLANNING state to update assignments.");

        var delta = request.CommittedHours - assignment.CommittedHours;
        if (delta > 0)
        {
            var usedByMember = await _assignments.GetTotalHoursForMemberPlanAsync(assignment.MemberPlanId, cancellationToken);
            var newTotal = usedByMember - assignment.CommittedHours + request.CommittedHours;
            if (newTotal > 30m)
            {
                var canAdd = 30m - (usedByMember - assignment.CommittedHours);
                return (null, $"You only have {canAdd} hours you can set here.");
            }

            var categoryUsed = await _assignments.GetCategoryHoursUsedAsync(assignment.MemberPlan.CycleId, assignment.BacklogItem!.Category, cancellationToken);
            var newCatTotal = categoryUsed - assignment.CommittedHours + request.CommittedHours;
            var allocation = assignment.MemberPlan.Cycle.CategoryAllocations?.FirstOrDefault(ca => ca.Category == assignment.BacklogItem.Category);
            if (allocation is not null && newCatTotal > allocation.BudgetHours)
            {
                var catRemaining = allocation.BudgetHours - (categoryUsed - assignment.CommittedHours);
                return (null, $"The {assignment.BacklogItem.Category} budget only has {catRemaining} hours left.");
            }
        }

        assignment.CommittedHours = request.CommittedHours;
        await _assignments.UpdateAsync(assignment, cancellationToken);

        var memberPlan = await _memberPlans.GetByIdAsync(assignment.MemberPlanId, cancellationToken);
        if (memberPlan is not null)
        {
            var total = (await _assignments.GetByMemberPlanIdAsync(assignment.MemberPlanId, cancellationToken)).Sum(a => a.CommittedHours);
            memberPlan.TotalPlannedHours = total;
            memberPlan.IsReady = false;
            await _memberPlans.UpdateAsync(memberPlan, cancellationToken);
        }

        return (ToDto(assignment), null);
    }

    public async Task<string?> DeleteAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
            return "Assignment not found.";
        if (assignment.MemberPlan?.Cycle?.State != "PLANNING")
            return "Cycle must be in PLANNING state to remove assignments.";

        var memberPlanId = assignment.MemberPlanId;
        var backlogItemId = assignment.BacklogItemId;
        var cycleId = assignment.MemberPlan.CycleId;

        await _assignments.DeleteAsync(assignmentId, cancellationToken);

        var otherInCycle = (await _assignments.GetByCycleIdAsync(cycleId, cancellationToken))
            .Any(a => a.BacklogItemId == backlogItemId);
        if (!otherInCycle)
        {
            var item = await _backlog.GetByIdAsync(backlogItemId, cancellationToken);
            if (item is not null)
            {
                item.Status = "AVAILABLE";
                await _backlog.UpdateAsync(item, cancellationToken);
            }
        }

        var memberPlan = await _memberPlans.GetByIdAsync(memberPlanId, cancellationToken);
        if (memberPlan is not null)
        {
            var total = (await _assignments.GetByMemberPlanIdAsync(memberPlanId, cancellationToken)).Sum(a => a.CommittedHours);
            memberPlan.TotalPlannedHours = total;
            memberPlan.IsReady = false;
            await _memberPlans.UpdateAsync(memberPlan, cancellationToken);
        }

        return null;
    }

    public async Task<AssignmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var a = await _assignments.GetByIdAsync(id, cancellationToken);
        return a is null ? null : ToDto(a);
    }

    private static AssignmentDto ToDto(TaskAssignment a) => new()
    {
        Id = a.Id,
        MemberPlanId = a.MemberPlanId,
        BacklogItemId = a.BacklogItemId,
        BacklogItemTitle = a.BacklogItem?.Title ?? "",
        Category = a.BacklogItem?.Category ?? "",
        CommittedHours = a.CommittedHours,
        ProgressStatus = a.ProgressStatus,
        HoursCompleted = a.HoursCompleted
    };
}

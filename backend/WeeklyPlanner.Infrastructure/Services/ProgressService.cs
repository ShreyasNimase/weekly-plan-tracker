using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.Infrastructure.Services;

public class ProgressService : IProgressService
{
    private static readonly HashSet<string> ValidStatuses = ["NOT_STARTED", "IN_PROGRESS", "COMPLETED", "BLOCKED"];

    // Allowed transitions: from -> to[]
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NOT_STARTED"] = ["NOT_STARTED", "IN_PROGRESS", "BLOCKED"],
        ["IN_PROGRESS"] = ["IN_PROGRESS", "COMPLETED", "BLOCKED"],
        ["BLOCKED"] = ["BLOCKED", "IN_PROGRESS"],
        ["COMPLETED"] = ["COMPLETED", "IN_PROGRESS"]
    };

    private readonly ITaskAssignmentRepository _assignments;
    private readonly IProgressRepository _progressUpdates;
    private readonly ICycleRepository _cycles;
    private readonly IMemberPlanRepository _memberPlans;

    public ProgressService(
        ITaskAssignmentRepository assignments,
        IProgressRepository progressUpdates,
        ICycleRepository cycles,
        IMemberPlanRepository memberPlans)
    {
        _assignments = assignments;
        _progressUpdates = progressUpdates;
        _cycles = cycles;
        _memberPlans = memberPlans;
    }

    public async Task<(AssignmentDto? Result, string? Error)> UpdateProgressAsync(Guid assignmentId, UpdateProgressRequest request, Guid? currentUserId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
            return (null, "Assignment not found.");
        if (assignment.MemberPlan?.Cycle?.State != "FROZEN")
            return (null, "Cycle must be in FROZEN state to update progress.");

        if (request.HoursCompleted < 0 || request.HoursCompleted % 0.5m != 0)
            return (null, "Hours completed must be >= 0 and in 0.5 increments.");

        var requestedStatus = (request.ProgressStatus ?? "").Trim();
        if (string.IsNullOrEmpty(requestedStatus) || !ValidStatuses.Contains(requestedStatus))
            return (null, "ProgressStatus must be one of: NOT_STARTED, IN_PROGRESS, COMPLETED, BLOCKED.");

        var note = request.Note?.Length > 1000 ? request.Note[..1000] : request.Note;

        var currentStatus = assignment.ProgressStatus;
        var effectiveStatus = requestedStatus;

        // Auto-status: hoursCompleted > 0 and current NOT_STARTED and requested NOT_STARTED → IN_PROGRESS
        if (request.HoursCompleted > 0 && string.Equals(currentStatus, "NOT_STARTED", StringComparison.OrdinalIgnoreCase)
            && string.Equals(requestedStatus, "NOT_STARTED", StringComparison.OrdinalIgnoreCase))
            effectiveStatus = "IN_PROGRESS";

        // Invalid transition blocks with specific message
        if (string.Equals(currentStatus, "NOT_STARTED", StringComparison.OrdinalIgnoreCase)
            && string.Equals(effectiveStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            return (null, "Please set this to In Progress first.");
        if (string.Equals(currentStatus, "BLOCKED", StringComparison.OrdinalIgnoreCase)
            && string.Equals(effectiveStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            return (null, "Please set this to In Progress first.");

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(effectiveStatus, StringComparer.OrdinalIgnoreCase))
            return (null, $"Invalid status transition from {currentStatus} to {effectiveStatus}.");

        var previousHours = assignment.HoursCompleted;
        var previousStatus = assignment.ProgressStatus;

        assignment.HoursCompleted = request.HoursCompleted;
        assignment.ProgressStatus = effectiveStatus;
        await _assignments.UpdateAsync(assignment, cancellationToken);

        var updatedBy = currentUserId ?? assignment.MemberPlan?.MemberId ?? Guid.Empty;
        var progressUpdate = new ProgressUpdate
        {
            TaskAssignmentId = assignmentId,
            PreviousHoursCompleted = previousHours,
            NewHoursCompleted = request.HoursCompleted,
            PreviousStatus = previousStatus,
            NewStatus = effectiveStatus,
            Note = note,
            Timestamp = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };
        await _progressUpdates.AddAsync(progressUpdate, cancellationToken);

        return (ToAssignmentDto(assignment), null);
    }

    public async Task<CycleProgressDto?> GetCycleProgressAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null) return null;

        var memberPlans = cycle.MemberPlans ?? (await _memberPlans.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();
        var allAssignments = (await _assignments.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();
        var byMemberPlan = allAssignments.GroupBy(a => a.MemberPlanId).ToDictionary(g => g.Key, g => g.ToList());

        var members = memberPlans.Select(mp =>
        {
            var assignments = byMemberPlan.GetValueOrDefault(mp.Id, []);
            return new MemberProgressItemDto
            {
                MemberId = mp.MemberId,
                MemberName = mp.Member?.Name ?? "",
                IsReady = mp.IsReady,
                TotalPlanned = mp.TotalPlannedHours,
                Assignments = assignments.Select(a => new AssignmentProgressItemDto
                {
                    Id = a.Id,
                    BacklogItemId = a.BacklogItemId,
                    Title = a.BacklogItem?.Title ?? "",
                    Category = a.BacklogItem?.Category ?? "",
                    CommittedHours = a.CommittedHours,
                    HoursCompleted = a.HoursCompleted,
                    ProgressStatus = a.ProgressStatus
                }).ToList()
            };
        }).ToList();

        return new CycleProgressDto { Members = members };
    }

    public async Task<MemberProgressDetailDto?> GetMemberProgressAsync(Guid cycleId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null) return null;

        var memberPlan = (cycle.MemberPlans ?? (await _memberPlans.GetByCycleIdAsync(cycleId, cancellationToken)).ToList())
            .FirstOrDefault(mp => mp.MemberId == memberId);
        if (memberPlan is null) return null;

        var assignments = await _assignments.GetByMemberPlanIdAsync(memberPlan.Id, cancellationToken);
        var assignmentDtos = new List<AssignmentWithHistoryDto>();
        foreach (var a in assignments)
        {
            var updates = (await _progressUpdates.GetByTaskAssignmentIdAsync(a.Id, cancellationToken)).ToList();
            assignmentDtos.Add(new AssignmentWithHistoryDto
            {
                Id = a.Id,
                BacklogItemId = a.BacklogItemId,
                Title = a.BacklogItem?.Title ?? "",
                Category = a.BacklogItem?.Category ?? "",
                CommittedHours = a.CommittedHours,
                HoursCompleted = a.HoursCompleted,
                ProgressStatus = a.ProgressStatus,
                ProgressUpdates = updates.Select(u => new ProgressUpdateItemDto
                {
                    Id = u.Id,
                    PreviousHoursCompleted = u.PreviousHoursCompleted,
                    NewHoursCompleted = u.NewHoursCompleted,
                    PreviousStatus = u.PreviousStatus,
                    NewStatus = u.NewStatus,
                    Note = u.Note,
                    Timestamp = u.Timestamp,
                    UpdatedBy = u.UpdatedBy
                }).ToList()
            });
        }

        return new MemberProgressDetailDto
        {
            MemberId = memberPlan.MemberId,
            MemberName = memberPlan.Member?.Name ?? "",
            IsReady = memberPlan.IsReady,
            TotalPlanned = memberPlan.TotalPlannedHours,
            Assignments = assignmentDtos
        };
    }

    public async Task<IReadOnlyList<CategoryProgressDto>?> GetCategoryProgressAsync(Guid cycleId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycles.GetByIdAsync(cycleId, cancellationToken);
        if (cycle is null) return null;

        var allocations = cycle.CategoryAllocations ?? new List<CategoryAllocation>();
        var assignments = (await _assignments.GetByCycleIdAsync(cycleId, cancellationToken)).ToList();

        var result = allocations.Select(ca =>
        {
            var categoryAssignments = assignments.Where(a => a.BacklogItem?.Category == ca.Category).ToList();
            var totalCommitted = categoryAssignments.Sum(a => a.CommittedHours);
            var totalCompleted = categoryAssignments.Sum(a => a.HoursCompleted);
            var completedCount = categoryAssignments.Count(a => string.Equals(a.ProgressStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase));
            return new CategoryProgressDto
            {
                Category = ca.Category,
                BudgetHours = ca.BudgetHours,
                TotalCommitted = totalCommitted,
                TotalCompleted = totalCompleted,
                CompletedTaskCount = completedCount,
                TotalTaskCount = categoryAssignments.Count
            };
        }).ToList();

        return result;
    }

    private static AssignmentDto ToAssignmentDto(TaskAssignment a)
    {
        return new AssignmentDto
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
}

using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class ProgressServiceTests
{
    private readonly Mock<ITaskAssignmentRepository> _assignments;
    private readonly Mock<IProgressRepository> _progressUpdates;
    private readonly Mock<ICycleRepository> _cycles;
    private readonly Mock<IMemberPlanRepository> _memberPlans;
    private readonly ProgressService _service;

    public ProgressServiceTests()
    {
        _assignments = new Mock<ITaskAssignmentRepository>();
        _progressUpdates = new Mock<IProgressRepository>();
        _cycles = new Mock<ICycleRepository>();
        _memberPlans = new Mock<IMemberPlanRepository>();
        _service = new ProgressService(_assignments.Object, _progressUpdates.Object, _cycles.Object, _memberPlans.Object);
    }

    private static TaskAssignment CreateAssignment(string status, decimal hoursCompleted = 0)
    {
        var cycle = new PlanningCycle { Id = Guid.NewGuid(), State = "FROZEN" };
        var memberPlan = new MemberPlan { CycleId = cycle.Id, Cycle = cycle };
        return new TaskAssignment
        {
            Id = Guid.NewGuid(),
            MemberPlanId = Guid.NewGuid(),
            MemberPlan = memberPlan,
            BacklogItemId = Guid.NewGuid(),
            BacklogItem = new BacklogItem { Title = "Task", Category = "A" },
            ProgressStatus = status,
            HoursCompleted = hoursCompleted
        };
    }

    [Fact]
    public async Task UpdateProgress_NotStartedToCompleted_ReturnsPleaseSetInProgressFirst()
    {
        var assignment = CreateAssignment("NOT_STARTED");
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignments.Setup(a => a.UpdateAsync(It.IsAny<TaskAssignment>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskAssignment t, CancellationToken _) => t);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "COMPLETED", HoursCompleted = 10 }, null);
        Assert.Null(result);
        Assert.Equal("Please set this to In Progress first.", error);
    }

    [Fact]
    public async Task UpdateProgress_BlockedToCompleted_ReturnsPleaseSetInProgressFirst()
    {
        var assignment = CreateAssignment("BLOCKED");
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "COMPLETED", HoursCompleted = 5 }, null);
        Assert.Null(result);
        Assert.Equal("Please set this to In Progress first.", error);
    }

    [Fact]
    public async Task UpdateProgress_InvalidTransition_ReturnsInvalidStatusTransition()
    {
        // COMPLETED -> BLOCKED is not allowed (only COMPLETED, IN_PROGRESS allowed from COMPLETED)
        var assignment = CreateAssignment("COMPLETED");
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "BLOCKED", HoursCompleted = 10 }, null);
        Assert.Null(result);
        Assert.Contains("Invalid status transition", error);
        Assert.Contains("COMPLETED", error);
        Assert.Contains("BLOCKED", error);
    }

    [Fact]
    public async Task UpdateProgress_NotStartedToInProgress_Succeeds()
    {
        var assignment = CreateAssignment("NOT_STARTED");
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignments.Setup(a => a.UpdateAsync(It.IsAny<TaskAssignment>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskAssignment t, CancellationToken _) => t);
        _progressUpdates.Setup(p => p.AddAsync(It.IsAny<ProgressUpdate>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProgressUpdate u, CancellationToken _) => u);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "IN_PROGRESS", HoursCompleted = 2.5m }, Guid.NewGuid());
        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal("IN_PROGRESS", result.ProgressStatus);
        Assert.Equal(2.5m, result.HoursCompleted);
    }

    [Fact]
    public async Task UpdateProgress_CycleNotFrozen_ReturnsError()
    {
        var assignment = CreateAssignment("NOT_STARTED");
        assignment.MemberPlan!.Cycle!.State = "PLANNING";
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "IN_PROGRESS", HoursCompleted = 1 }, null);
        Assert.Null(result);
        Assert.Contains("FROZEN", error);
    }

    [Fact]
    public async Task UpdateProgress_HoursNotHalfStep_ReturnsError()
    {
        var assignment = CreateAssignment("IN_PROGRESS");
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "IN_PROGRESS", HoursCompleted = 1.25m }, null);
        Assert.Null(result);
        Assert.Contains("0.5", error);
    }

    [Fact]
    public async Task UpdateProgress_AutoStatus_NotStartedWithHoursAndStatusNotStarted_BecomesInProgress()
    {
        var assignment = CreateAssignment("NOT_STARTED");
        _assignments.Setup(a => a.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignments.Setup(a => a.UpdateAsync(It.IsAny<TaskAssignment>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskAssignment t, CancellationToken _) => t);
        _progressUpdates.Setup(p => p.AddAsync(It.IsAny<ProgressUpdate>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProgressUpdate u, CancellationToken _) => u);

        var (result, error) = await _service.UpdateProgressAsync(assignment.Id, new UpdateProgressRequest { ProgressStatus = "NOT_STARTED", HoursCompleted = 3 }, null);
        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal("IN_PROGRESS", result.ProgressStatus);
    }
}

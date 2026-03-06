using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class DashboardServiceTests
{
    private readonly Mock<ICycleRepository> _cycles;
    private readonly Mock<ITaskAssignmentRepository> _assignments;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _cycles = new Mock<ICycleRepository>();
        _assignments = new Mock<ITaskAssignmentRepository>();
        _service = new DashboardService(_cycles.Object, _assignments.Object);
    }

    [Fact]
    public async Task GetDashboard_NoActiveCycle_ReturnsEmptyDto()
    {
        _cycles.Setup(c => c.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync((PlanningCycle?)null);
        var result = await _service.GetDashboardAsync();
        Assert.NotNull(result);
        Assert.Null(result.CycleId);
        Assert.Null(result.State);
        Assert.Equal(0, result.TotalTaskCount);
        Assert.Empty(result.MemberBreakdown);
        Assert.Empty(result.CategoryBreakdown);
    }

    [Fact]
    public async Task GetDashboard_WithCycle_CalculatesTotalsAndBreakdown()
    {
        var cycleId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var cycle = new PlanningCycle
        {
            Id = cycleId,
            State = "FROZEN",
            PlanningDate = new DateTime(2025, 3, 4),
            TeamCapacity = 120,
            CategoryAllocations = new List<CategoryAllocation> { new() { Category = "CLIENT", BudgetHours = 60 } },
            MemberPlans = new List<MemberPlan> { new() { Id = Guid.NewGuid(), MemberId = memberId, Member = new TeamMember { Name = "Alice" }, TotalPlannedHours = 30 } }
        };
        var assignments = new List<TaskAssignment>
        {
            new() { Id = Guid.NewGuid(), MemberPlanId = cycle.MemberPlans[0].Id, MemberPlan = cycle.MemberPlans[0], BacklogItem = new BacklogItem { Category = "CLIENT" }, CommittedHours = 10, HoursCompleted = 10, ProgressStatus = "COMPLETED" },
            new() { Id = Guid.NewGuid(), MemberPlanId = cycle.MemberPlans[0].Id, MemberPlan = cycle.MemberPlans[0], BacklogItem = new BacklogItem { Category = "CLIENT" }, CommittedHours = 20, HoursCompleted = 5, ProgressStatus = "IN_PROGRESS" }
        };
        _cycles.Setup(c => c.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cycle);
        _assignments.Setup(a => a.GetByCycleIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(assignments);

        var result = await _service.GetDashboardAsync();
        Assert.NotNull(result);
        Assert.Equal(cycleId, result.CycleId);
        Assert.Equal("FROZEN", result.State);
        Assert.Equal(15, result.TotalCompleted);
        Assert.Equal(1, result.CompletedTaskCount);
        Assert.Equal(2, result.TotalTaskCount);
        Assert.Single(result.CategoryBreakdown);
        Assert.Equal(60, result.CategoryBreakdown[0].BudgetHours);
        Assert.Equal(30, result.CategoryBreakdown[0].PlannedHours);
        Assert.Equal(15, result.CategoryBreakdown[0].CompletedHours);
        Assert.Single(result.MemberBreakdown);
        Assert.Equal(30, result.MemberBreakdown[0].PlannedHours);
        Assert.Equal(15, result.MemberBreakdown[0].CompletedHours);
        Assert.Equal(1, result.MemberBreakdown[0].CompletedTaskCount);
        Assert.Equal(2, result.MemberBreakdown[0].TaskCount);
    }
}

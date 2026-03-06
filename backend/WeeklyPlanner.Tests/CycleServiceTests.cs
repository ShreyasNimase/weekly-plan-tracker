using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class CycleServiceTests
{
    private readonly Mock<ICycleRepository> _cycles;
    private readonly Mock<ITeamMemberRepository> _members;
    private readonly Mock<IMemberPlanRepository> _memberPlans;
    private readonly Mock<IBacklogRepository> _backlog;
    private readonly Mock<ITaskAssignmentRepository> _assignments;
    private readonly CycleService _service;

    public CycleServiceTests()
    {
        _cycles = new Mock<ICycleRepository>();
        _members = new Mock<ITeamMemberRepository>();
        _memberPlans = new Mock<IMemberPlanRepository>();
        _backlog = new Mock<IBacklogRepository>();
        _assignments = new Mock<ITaskAssignmentRepository>();
        _service = new CycleService(_cycles.Object, _members.Object, _memberPlans.Object, _backlog.Object, _assignments.Object);
    }

    [Fact]
    public async Task Setup_PlanningDateNotTuesday_ReturnsError()
    {
        var cycleId = Guid.NewGuid();
        var cycle = new PlanningCycle { Id = cycleId, State = "SETUP" };
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(cycle);

        var request = new SetupCycleRequest
        {
            PlanningDate = new DateTime(2025, 3, 3), // Monday
            MemberIds = new List<Guid> { Guid.NewGuid() },
            CategoryAllocations = new List<CategoryAllocationInputDto>
            {
                new() { Category = "A", Percentage = 34 },
                new() { Category = "B", Percentage = 33 },
                new() { Category = "C", Percentage = 33 }
            }
        };
        _members.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TeamMember { IsActive = true });

        var (result, error) = await _service.SetupAsync(cycleId, request);
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("Tuesday", error);
    }

    [Fact]
    public async Task Setup_NotExactlyThreeCategoryAllocations_ReturnsError()
    {
        var cycleId = Guid.NewGuid();
        var cycle = new PlanningCycle { Id = cycleId, State = "SETUP" };
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(cycle);
        var request = new SetupCycleRequest
        {
            PlanningDate = new DateTime(2025, 3, 4), // Tuesday
            MemberIds = new List<Guid> { Guid.NewGuid() },
            CategoryAllocations = new List<CategoryAllocationInputDto>
            {
                new() { Category = "A", Percentage = 50 },
                new() { Category = "B", Percentage = 50 }
            }
        };
        _members.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TeamMember { IsActive = true });

        var (result, error) = await _service.SetupAsync(cycleId, request);
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("exactly 3", error);
    }

    [Fact]
    public async Task Setup_PercentagesDoNotSumTo100_ReturnsError()
    {
        var cycleId = Guid.NewGuid();
        var cycle = new PlanningCycle { Id = cycleId, State = "SETUP" };
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(cycle);
        var request = new SetupCycleRequest
        {
            PlanningDate = new DateTime(2025, 3, 4),
            MemberIds = new List<Guid> { Guid.NewGuid() },
            CategoryAllocations = new List<CategoryAllocationInputDto>
            {
                new() { Category = "A", Percentage = 50 },
                new() { Category = "B", Percentage = 30 },
                new() { Category = "C", Percentage = 10 }
            }
        };
        _members.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TeamMember { IsActive = true });

        var (result, error) = await _service.SetupAsync(cycleId, request);
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("100", error);
    }

    [Fact]
    public async Task Freeze_MemberTotalPlannedHoursNot30_ReturnsErrors()
    {
        var cycleId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var cycle = new PlanningCycle
        {
            Id = cycleId,
            State = "PLANNING",
            CategoryAllocations = new List<CategoryAllocation> { new() { Category = "A", BudgetHours = 30 } }
        };
        var memberPlan = new MemberPlan { Id = Guid.NewGuid(), MemberId = memberId, TotalPlannedHours = 25, Member = new TeamMember { Name = "Alice" } };
        cycle.MemberPlans = new List<MemberPlan> { memberPlan };
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(cycle);
        _assignments.Setup(a => a.GetByCycleIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TaskAssignment>());

        var (result, errors) = await _service.FreezeAsync(cycleId);
        Assert.Null(result);
        Assert.NotNull(errors);
        Assert.True(errors.Count > 0);
        Assert.Contains(errors, e => e.Contains("TotalPlannedHours") && e.Contains("25") && e.Contains("30"));
    }

    [Fact]
    public async Task Freeze_CategoryCommittedNotEqualToBudget_ReturnsErrors()
    {
        var cycleId = Guid.NewGuid();
        var cycle = new PlanningCycle
        {
            Id = cycleId,
            State = "PLANNING",
            CategoryAllocations = new List<CategoryAllocation> { new() { Category = "CLIENT", BudgetHours = 30 } },
            MemberPlans = new List<MemberPlan> { new MemberPlan { TotalPlannedHours = 30 } }
        };
        var assignments = new List<TaskAssignment>
        {
            new() { BacklogItem = new BacklogItem { Category = "CLIENT" }, CommittedHours = 20 }
        };
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(cycle);
        _assignments.Setup(a => a.GetByCycleIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(assignments);

        var (result, errors) = await _service.FreezeAsync(cycleId);
        Assert.Null(result);
        Assert.NotNull(errors);
        Assert.Contains(errors, e => e.Contains("CLIENT") && e.Contains("20") && e.Contains("30"));
    }

    [Fact]
    public async Task Freeze_AllValid_Succeeds()
    {
        var cycleId = Guid.NewGuid();
        var cycle = new PlanningCycle
        {
            Id = cycleId,
            State = "PLANNING",
            CategoryAllocations = new List<CategoryAllocation> { new() { Category = "CLIENT", BudgetHours = 30 } },
            MemberPlans = new List<MemberPlan> { new MemberPlan { TotalPlannedHours = 30 } }
        };
        var assignments = new List<TaskAssignment>
        {
            new() { BacklogItem = new BacklogItem { Category = "CLIENT" }, CommittedHours = 30 }
        };
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(cycle);
        _assignments.Setup(a => a.GetByCycleIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync(assignments);
        _cycles.Setup(c => c.UpdateAsync(It.IsAny<PlanningCycle>(), It.IsAny<CancellationToken>())).ReturnsAsync((PlanningCycle c, CancellationToken _) => c);

        var (result, errors) = await _service.FreezeAsync(cycleId);
        Assert.NotNull(result);
        Assert.Null(errors);
        Assert.Equal("FROZEN", cycle.State);
    }

    [Fact]
    public async Task Freeze_CycleNotFound_ReturnsNotFoundError()
    {
        var cycleId = Guid.NewGuid();
        _cycles.Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>())).ReturnsAsync((PlanningCycle?)null);

        var (result, errors) = await _service.FreezeAsync(cycleId);
        Assert.Null(result);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Cycle not found.", errors[0]);
    }
}

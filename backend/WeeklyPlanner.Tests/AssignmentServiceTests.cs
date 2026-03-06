using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class AssignmentServiceTests
{
    private readonly Mock<ITaskAssignmentRepository> _assignments;
    private readonly Mock<IMemberPlanRepository> _memberPlans;
    private readonly Mock<IBacklogRepository> _backlog;
    private readonly AssignmentService _service;

    public AssignmentServiceTests()
    {
        _assignments = new Mock<ITaskAssignmentRepository>();
        _memberPlans = new Mock<IMemberPlanRepository>();
        _backlog = new Mock<IBacklogRepository>();
        _service = new AssignmentService(_assignments.Object, _memberPlans.Object, _backlog.Object);
    }

    [Fact]
    public async Task Create_ExceedsMember30hCap_ReturnsExactMessage()
    {
        var memberPlanId = Guid.NewGuid();
        var backlogItemId = Guid.NewGuid();
        var cycle = new PlanningCycle { Id = Guid.NewGuid(), State = "PLANNING", CategoryAllocations = new List<CategoryAllocation> { new() { Category = "CLIENT", BudgetHours = 30 } } };
        var memberPlan = new MemberPlan { Id = memberPlanId, CycleId = cycle.Id, Cycle = cycle };
        var backlogItem = new BacklogItem { Id = backlogItemId, Category = "CLIENT", Status = "AVAILABLE" };

        _memberPlans.Setup(m => m.GetByIdAsync(memberPlanId, It.IsAny<CancellationToken>())).ReturnsAsync(memberPlan);
        _backlog.Setup(b => b.GetByIdAsync(backlogItemId, It.IsAny<CancellationToken>())).ReturnsAsync(backlogItem);
        _assignments.Setup(a => a.GetTotalHoursForMemberPlanAsync(memberPlanId, It.IsAny<CancellationToken>())).ReturnsAsync(28m);
        _assignments.Setup(a => a.GetCategoryHoursUsedAsync(cycle.Id, "CLIENT", It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        // 28 + 5 = 33 > 30, remaining = 2
        var (result, error) = await _service.CreateAsync(new CreateAssignmentRequest { MemberPlanId = memberPlanId, BacklogItemId = backlogItemId, CommittedHours = 5m });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal("You only have 2 hours left.", error);
    }

    [Fact]
    public async Task Create_ExceedsCategoryBudget_ReturnsExactMessage()
    {
        var memberPlanId = Guid.NewGuid();
        var backlogItemId = Guid.NewGuid();
        var cycle = new PlanningCycle
        {
            Id = Guid.NewGuid(),
            State = "PLANNING",
            CategoryAllocations = new List<CategoryAllocation> { new() { Category = "TECH_DEBT", BudgetHours = 10 } }
        };
        var memberPlan = new MemberPlan { Id = memberPlanId, CycleId = cycle.Id, Cycle = cycle };
        var backlogItem = new BacklogItem { Id = backlogItemId, Category = "TECH_DEBT", Status = "AVAILABLE" };

        _memberPlans.Setup(m => m.GetByIdAsync(memberPlanId, It.IsAny<CancellationToken>())).ReturnsAsync(memberPlan);
        _backlog.Setup(b => b.GetByIdAsync(backlogItemId, It.IsAny<CancellationToken>())).ReturnsAsync(backlogItem);
        _assignments.Setup(a => a.GetTotalHoursForMemberPlanAsync(memberPlanId, It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        _assignments.Setup(a => a.GetCategoryHoursUsedAsync(cycle.Id, "TECH_DEBT", It.IsAny<CancellationToken>())).ReturnsAsync(8m);
        // 8 + 5 = 13 > 10, category remaining = 2
        var (result, error) = await _service.CreateAsync(new CreateAssignmentRequest { MemberPlanId = memberPlanId, BacklogItemId = backlogItemId, CommittedHours = 5m });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal("The TECH_DEBT budget only has 2 hours left.", error);
    }

    [Fact]
    public async Task Update_IncreasingHoursExceeds30h_ReturnsExactMessage()
    {
        var assignmentId = Guid.NewGuid();
        var memberPlanId = Guid.NewGuid();
        var cycle = new PlanningCycle { Id = Guid.NewGuid(), State = "PLANNING", CategoryAllocations = new List<CategoryAllocation>() };
        var memberPlan = new MemberPlan { Id = memberPlanId, CycleId = cycle.Id, Cycle = cycle };
        var assignment = new TaskAssignment
        {
            Id = assignmentId,
            MemberPlanId = memberPlanId,
            CommittedHours = 10m,
            BacklogItem = new BacklogItem { Category = "A" },
            MemberPlan = memberPlan
        };
        _assignments.Setup(a => a.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        // Total for member = 10 + 15 = 25, so 5 more allowed. If we set to 20, new total = 25 - 10 + 20 = 35. canAdd = 30 - 15 = 15. Message: "You only have 15 hours you can set here."
        _assignments.Setup(a => a.GetTotalHoursForMemberPlanAsync(memberPlanId, It.IsAny<CancellationToken>())).ReturnsAsync(25m);
        _assignments.Setup(a => a.GetCategoryHoursUsedAsync(cycle.Id, "A", It.IsAny<CancellationToken>())).ReturnsAsync(25m);

        var (result, error) = await _service.UpdateAsync(assignmentId, new UpdateAssignmentRequest { CommittedHours = 20m });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal("You only have 15 hours you can set here.", error);
    }

    [Fact]
    public async Task Update_IncreasingHoursExceedsCategoryBudget_ReturnsExactMessage()
    {
        var assignmentId = Guid.NewGuid();
        var memberPlanId = Guid.NewGuid();
        var cycle = new PlanningCycle
        {
            Id = Guid.NewGuid(),
            State = "PLANNING",
            CategoryAllocations = new List<CategoryAllocation> { new() { Category = "INTERNAL", BudgetHours = 20 } }
        };
        var memberPlan = new MemberPlan { Id = memberPlanId, CycleId = cycle.Id, Cycle = cycle };
        var assignment = new TaskAssignment
        {
            Id = assignmentId,
            MemberPlanId = memberPlanId,
            CommittedHours = 5m,
            BacklogItem = new BacklogItem { Category = "INTERNAL" },
            MemberPlan = memberPlan
        };
        _assignments.Setup(a => a.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignments.Setup(a => a.GetTotalHoursForMemberPlanAsync(memberPlanId, It.IsAny<CancellationToken>())).ReturnsAsync(10m);
        // Category used = 15 (5 from this + 10 others). New total if we set 10 = 15 - 5 + 10 = 20. Budget 20, so exactly at limit. Use 12: category used 15, new = 15-5+12=22 > 20. Remaining for category = 20 - 10 = 10.
        _assignments.Setup(a => a.GetCategoryHoursUsedAsync(cycle.Id, "INTERNAL", It.IsAny<CancellationToken>())).ReturnsAsync(15m);

        var (result, error) = await _service.UpdateAsync(assignmentId, new UpdateAssignmentRequest { CommittedHours = 12m });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal("The INTERNAL budget only has 10 hours left.", error);
    }
}

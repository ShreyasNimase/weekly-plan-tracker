using Microsoft.AspNetCore.Mvc;
using Moq;
using WeeklyPlanner.API.Controllers;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.Tests;

public class ProgressControllerTests
{
    private readonly Mock<ICycleRepository> _cycleRepo;
    private readonly Mock<ITaskAssignmentRepository> _assignRepo;
    private readonly Mock<IMemberPlanRepository> _memberPlanRepo;
    private readonly Mock<ITeamMemberRepository> _memberRepo;
    private readonly Mock<IBacklogRepository> _backlogRepo;
    private readonly ProgressController _progressCtrl;
    private readonly DashboardController _dashboardCtrl;

    public ProgressControllerTests()
    {
        _cycleRepo = new Mock<ICycleRepository>();
        _assignRepo = new Mock<ITaskAssignmentRepository>();
        _memberPlanRepo = new Mock<IMemberPlanRepository>();
        _memberRepo = new Mock<ITeamMemberRepository>();
        _backlogRepo = new Mock<IBacklogRepository>();

        _progressCtrl = new ProgressController(_cycleRepo.Object, _assignRepo.Object, _memberPlanRepo.Object);
        _dashboardCtrl = new DashboardController(
            _cycleRepo.Object, _memberRepo.Object, _backlogRepo.Object, _assignRepo.Object);
    }

    // ─── helpers ────────────────────────────────────────────────
    private static TeamMember MakeMember(bool lead = false)
        => new() { Id = Guid.NewGuid(), Name = "Alice", IsActive = true, IsLead = lead };

    private static PlanningCycle MakeCycleWithMembers(CycleStatus status = CycleStatus.Planning)
    {
        var cycle = new PlanningCycle
        {
            Id            = Guid.NewGuid(),
            WeekStartDate = DateTime.Today,
            Status        = status,
            CycleMembers  = [],
            CategoryBudgets = []
        };
        var tm = MakeMember();
        var cm = new CycleMember
        {
            Id = Guid.NewGuid(), CycleId = cycle.Id, TeamMemberId = tm.Id,
            TeamMember = tm, AllocatedHours = 30m, IsReady = false, TaskAssignments = []
        };
        cycle.CycleMembers.Add(cm);
        cycle.CategoryBudgets.Add(new CategoryBudget
        {
            Id = Guid.NewGuid(), CycleId = cycle.Id,
            Category = BacklogCategory.Feature, Percentage = 100m, HoursBudget = 30m
        });
        return cycle;
    }

    private static TaskAssignment MakeAssignment(CycleMember cm)
    {
        var item = new BacklogItem { Id = Guid.NewGuid(), Title = "Task", Category = BacklogCategory.Feature, Status = BacklogStatus.Active };
        return new TaskAssignment { Id = Guid.NewGuid(), CycleMemberId = cm.Id, CycleMember = cm, BacklogItemId = item.Id, BacklogItem = item, PlannedHours = 5m };
    }

    // ────────────────────────────────────────────────────────────
    // 25. GET /api/cycles/{id}/progress
    // ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetCycleProgress_Found_Returns200WithBreakdown()
    {
        var cycle = MakeCycleWithMembers();
        var cm    = cycle.CycleMembers[0];
        var asgn  = new List<TaskAssignment> { MakeAssignment(cm) };

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _assignRepo.Setup(r => r.GetCycleAssignmentsAsync(cycle.Id)).ReturnsAsync(asgn);

        var result = await _progressCtrl.GetCycleProgress(cycle.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCycleProgress_NotFound_Returns404()
    {
        _cycleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PlanningCycle?)null);

        var result = await _progressCtrl.GetCycleProgress(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────
    // 26. GET /api/cycles/{id}/members/{cycleMemberId}/progress
    // ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetMemberProgress_Found_Returns200WithTasks()
    {
        var cycle = MakeCycleWithMembers();
        var cm    = cycle.CycleMembers[0];
        var asgns = new List<TaskAssignment> { MakeAssignment(cm) };

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _assignRepo.Setup(r => r.GetMemberAssignmentsAsync(cm.Id)).ReturnsAsync(asgns);

        var result = await _progressCtrl.GetMemberProgress(cycle.Id, cm.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMemberProgress_MemberNotInCycle_Returns404()
    {
        var cycle = MakeCycleWithMembers();

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);

        var result = await _progressCtrl.GetMemberProgress(cycle.Id, Guid.NewGuid()); // random ID

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────
    // 27. GET /api/cycles/{id}/category-progress
    // ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetCategoryProgress_Found_Returns200()
    {
        var cycle = MakeCycleWithMembers();
        var asgns = new List<TaskAssignment> { MakeAssignment(cycle.CycleMembers[0]) };

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _assignRepo.Setup(r => r.GetCycleAssignmentsAsync(cycle.Id)).ReturnsAsync(asgns);

        var result = await _progressCtrl.GetCategoryProgress(cycle.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    // ────────────────────────────────────────────────────────────
    // 28. GET /api/dashboard
    // ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDashboard_WithActiveCycle_Returns200()
    {
        var cycle  = MakeCycleWithMembers();
        var member = MakeMember(lead: true);

        _cycleRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(cycle);
        _assignRepo.Setup(r => r.GetCycleAssignmentsAsync(cycle.Id))
                   .ReturnsAsync(new List<TaskAssignment>());
        _memberRepo.Setup(r => r.GetAllAsync())
                   .ReturnsAsync(new List<TeamMember> { member });
        _backlogRepo.Setup(r => r.GetAllAsync(null, BacklogStatus.Active, null))
                    .ReturnsAsync(new List<BacklogItem>());
        _backlogRepo.Setup(r => r.GetAllAsync(null, BacklogStatus.Archived, null))
                    .ReturnsAsync(new List<BacklogItem>());
        _cycleRepo.Setup(r => r.GetHistoryAsync())
                  .ReturnsAsync(new List<PlanningCycle>());

        var result = await _dashboardCtrl.GetDashboard();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDashboard_NoActiveCycle_Returns200WithNullActiveCycle()
    {
        _cycleRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((PlanningCycle?)null);
        _memberRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<TeamMember>());
        _backlogRepo.Setup(r => r.GetAllAsync(null, BacklogStatus.Active, null))
                    .ReturnsAsync(new List<BacklogItem>());
        _backlogRepo.Setup(r => r.GetAllAsync(null, BacklogStatus.Archived, null))
                    .ReturnsAsync(new List<BacklogItem>());
        _cycleRepo.Setup(r => r.GetHistoryAsync()).ReturnsAsync(new List<PlanningCycle>());

        var result = await _dashboardCtrl.GetDashboard();

        Assert.IsType<OkObjectResult>(result);
    }
}

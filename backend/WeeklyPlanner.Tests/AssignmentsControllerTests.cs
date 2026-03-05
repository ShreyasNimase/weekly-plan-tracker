using Microsoft.AspNetCore.Mvc;
using Moq;
using WeeklyPlanner.API.Controllers;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.Tests;

public class AssignmentsControllerTests
{
    private readonly Mock<IAssignmentRepository> _repo;
    private readonly Mock<ICycleRepository>      _cycles;
    private readonly Mock<IBacklogRepository>    _backlog;
    private readonly AssignmentsController       _controller;
    private readonly MemberPlansController       _memberPlansController;

    public AssignmentsControllerTests()
    {
        _repo    = new Mock<IAssignmentRepository>();
        _cycles  = new Mock<ICycleRepository>();
        _backlog = new Mock<IBacklogRepository>();
        _controller            = new AssignmentsController(_repo.Object, _cycles.Object, _backlog.Object);
        _memberPlansController = new MemberPlansController(_repo.Object);
    }

    // ─── helpers ────────────────────────────────────────────────
    private static CycleMember MakeMember(CycleStatus status = CycleStatus.Planning)
        => new()
        {
            Id           = Guid.NewGuid(),
            TeamMemberId = Guid.NewGuid(),
            TeamMember   = new TeamMember { Id = Guid.NewGuid(), Name = "Alice", IsActive = true },
            AllocatedHours = 30m,
            Cycle        = new PlanningCycle { Id = Guid.NewGuid(), Status = status, CategoryBudgets = [] },
            TaskAssignments = []
        };

    private static BacklogItem ActiveItem(BacklogCategory cat = BacklogCategory.Feature)
        => new() { Id = Guid.NewGuid(), Title = "Do work", Category = cat, Status = BacklogStatus.Active };

    private static TaskAssignment MakeAssignment(CycleMember cm, BacklogItem item, decimal hours = 5m)
        => new()
        {
            Id            = Guid.NewGuid(),
            CycleMemberId = cm.Id,
            CycleMember   = cm,
            BacklogItemId = item.Id,
            BacklogItem   = item,
            PlannedHours  = hours
        };

    // ─────────────────────────────────────────────────────────────
    // 21. POST /api/assignments — Claim backlog item
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task ClaimItem_Valid_Returns201()
    {
        var cm   = MakeMember();
        var item = ActiveItem();
        var dto  = new ClaimBacklogItemDto { CycleMemberId = cm.Id, BacklogItemId = item.Id, PlannedHours = 5m };

        _repo.Setup(r => r.GetCycleMemberByIdAsync(cm.Id)).ReturnsAsync(cm);
        _backlog.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _repo.Setup(r => r.IsBacklogItemClaimedInActiveCycleAsync(item.Id, Guid.Empty)).ReturnsAsync(false);
        _repo.Setup(r => r.GetTotalHoursForMemberAsync(cm.Id)).ReturnsAsync(0m);
        _repo.Setup(r => r.GetCategoryHoursUsedAsync(cm.Cycle.Id, "Feature")).ReturnsAsync(0m);
        _repo.Setup(r => r.AddAsync(It.IsAny<TaskAssignment>()))
             .ReturnsAsync((TaskAssignment a) => a);

        var result = await _controller.ClaimItem(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task ClaimItem_NonHalfHourIncrement_Returns400()
    {
        var dto = new ClaimBacklogItemDto { CycleMemberId = Guid.NewGuid(), BacklogItemId = Guid.NewGuid(), PlannedHours = 1.3m };

        var result = await _controller.ClaimItem(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task ClaimItem_ExceedsMemberHourCap_Returns400()
    {
        var cm   = MakeMember();
        var item = ActiveItem();
        var dto  = new ClaimBacklogItemDto { CycleMemberId = cm.Id, BacklogItemId = item.Id, PlannedHours = 5m };

        _repo.Setup(r => r.GetCycleMemberByIdAsync(cm.Id)).ReturnsAsync(cm);
        _backlog.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _repo.Setup(r => r.IsBacklogItemClaimedInActiveCycleAsync(item.Id, Guid.Empty)).ReturnsAsync(false);
        _repo.Setup(r => r.GetTotalHoursForMemberAsync(cm.Id)).ReturnsAsync(27m); // 27+5=32 > 30

        var result = await _controller.ClaimItem(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ClaimItem_CycleNotInPlanning_Returns400()
    {
        var cm  = MakeMember(CycleStatus.Setup);
        var dto = new ClaimBacklogItemDto { CycleMemberId = cm.Id, BacklogItemId = Guid.NewGuid(), PlannedHours = 5m };

        _repo.Setup(r => r.GetCycleMemberByIdAsync(cm.Id)).ReturnsAsync(cm);

        var result = await _controller.ClaimItem(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ClaimItem_BacklogItemArchived_Returns400()
    {
        var cm   = MakeMember();
        var item = new BacklogItem { Id = Guid.NewGuid(), Title = "Old", Category = BacklogCategory.Bug, Status = BacklogStatus.Archived };
        var dto  = new ClaimBacklogItemDto { CycleMemberId = cm.Id, BacklogItemId = item.Id, PlannedHours = 2m };

        _repo.Setup(r => r.GetCycleMemberByIdAsync(cm.Id)).ReturnsAsync(cm);
        _backlog.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _controller.ClaimItem(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 22. PUT /api/assignments/{id}
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateHours_Valid_Returns200()
    {
        var cm         = MakeMember();
        var item       = ActiveItem();
        var assignment = MakeAssignment(cm, item, 5m);

        _repo.Setup(r => r.GetByIdAsync(assignment.Id)).ReturnsAsync(assignment);
        _repo.Setup(r => r.GetTotalHoursForMemberAsync(cm.Id)).ReturnsAsync(10m);
        _repo.Setup(r => r.GetCategoryHoursUsedAsync(cm.Cycle.Id, "Feature")).ReturnsAsync(10m);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TaskAssignment>())).ReturnsAsync(assignment);

        var result = await _controller.UpdateHours(assignment.Id, new UpdateAssignmentDto { PlannedHours = 8m });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateHours_ExceedsCap_Returns400()
    {
        var cm         = MakeMember();
        var item       = ActiveItem();
        var assignment = MakeAssignment(cm, item, 5m);

        _repo.Setup(r => r.GetByIdAsync(assignment.Id)).ReturnsAsync(assignment);
        _repo.Setup(r => r.GetTotalHoursForMemberAsync(cm.Id)).ReturnsAsync(28m); // 28 - 5 + 10 = 33 > 30

        var result = await _controller.UpdateHours(assignment.Id, new UpdateAssignmentDto { PlannedHours = 10m });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 23. DELETE /api/assignments/{id}
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task RemoveAssignment_Found_Returns204()
    {
        var cm         = MakeMember();
        var item       = ActiveItem();
        var assignment = MakeAssignment(cm, item);

        _repo.Setup(r => r.GetByIdAsync(assignment.Id)).ReturnsAsync(assignment);
        _repo.Setup(r => r.DeleteAsync(assignment.Id)).Returns(Task.CompletedTask);

        var result = await _controller.RemoveAssignment(assignment.Id);

        Assert.IsType<NoContentResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 24. PUT /api/member-plans/{id}/ready
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task MarkPlanReady_WithTasks_Returns200()
    {
        var cm   = MakeMember();
        var item = ActiveItem();
        cm.TaskAssignments.Add(MakeAssignment(cm, item, 10m));

        _repo.Setup(r => r.GetCycleMemberByIdAsync(cm.Id)).ReturnsAsync(cm);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _memberPlansController.MarkReady(cm.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.True(cm.IsReady);
    }

    [Fact]
    public async Task MarkPlanReady_NoTasks_Returns400()
    {
        var cm = MakeMember(); // empty TaskAssignments

        _repo.Setup(r => r.GetCycleMemberByIdAsync(cm.Id)).ReturnsAsync(cm);

        var result = await _memberPlansController.MarkReady(cm.Id);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using Moq;
using WeeklyPlanner.API.Controllers;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.Tests;

public class CyclesControllerTests
{
    private readonly Mock<ICycleRepository>       _cycleRepo;
    private readonly Mock<ITeamMemberRepository>  _memberRepo;
    private readonly CyclesController             _controller;

    public CyclesControllerTests()
    {
        _cycleRepo  = new Mock<ICycleRepository>();
        _memberRepo = new Mock<ITeamMemberRepository>();
        _controller = new CyclesController(_cycleRepo.Object, _memberRepo.Object);
    }

    // ─── helpers ───────────────────────────────────────────────
    private static PlanningCycle MakeCycle(CycleStatus status = CycleStatus.Setup)
        => new()
        {
            Id            = Guid.NewGuid(),
            WeekStartDate = NextTuesday(),
            Status        = status,
            CycleMembers  = [],
            CategoryBudgets = []
        };

    private static DateTime NextTuesday()
    {
        var d = DateTime.Today;
        int daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)d.DayOfWeek + 7) % 7;
        return d.AddDays(daysUntilTuesday == 0 ? 7 : daysUntilTuesday);
    }

    private static TeamMember ActiveMember()
        => new() { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };

    private static SetupCycleDto ValidSetupDto(Guid memberId) => new()
    {
        MemberIds = [memberId],
        CategoryBudgets =
        [
            new CategoryBudgetDto { Category = BacklogCategory.Feature, Percentage = 60m },
            new CategoryBudgetDto { Category = BacklogCategory.Bug,     Percentage = 40m }
        ]
    };

    // ─────────────────────────────────────────────────────────────
    // 14. POST /api/cycles/start
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task StartCycle_TuesdayDate_Returns201()
    {
        _cycleRepo.Setup(r => r.HasActiveCycleAsync()).ReturnsAsync(false);
        _cycleRepo.Setup(r => r.AddAsync(It.IsAny<PlanningCycle>()))
                  .ReturnsAsync((PlanningCycle c) => c);

        var result = await _controller.StartCycle(new StartCycleDto { WeekStartDate = NextTuesday() });

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task StartCycle_NonTuesdayDate_Returns400()
    {
        // Use a Wednesday
        var wednesday = NextTuesday().AddDays(1);
        var result = await _controller.StartCycle(new StartCycleDto { WeekStartDate = wednesday });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task StartCycle_WhenActiveCycleExists_Returns400()
    {
        _cycleRepo.Setup(r => r.HasActiveCycleAsync()).ReturnsAsync(true);

        var result = await _controller.StartCycle(new StartCycleDto { WeekStartDate = NextTuesday() });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 15. PUT /api/cycles/{id}/setup
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task SetupCycle_ValidDto_Returns200()
    {
        var cycle  = MakeCycle(CycleStatus.Setup);
        var member = ActiveMember();

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _memberRepo.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _cycleRepo.Setup(r => r.SetupMembersAndBudgetsAsync(
                It.IsAny<PlanningCycle>(),
                It.IsAny<List<CycleMember>>(),
                It.IsAny<List<CategoryBudget>>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.SetupCycle(cycle.Id, ValidSetupDto(member.Id));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SetupCycle_PercentagesNot100_Returns400()
    {
        var cycle = MakeCycle(CycleStatus.Setup);
        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);

        var badDto = new SetupCycleDto
        {
            MemberIds = [Guid.NewGuid()],
            CategoryBudgets =
            [
                new CategoryBudgetDto { Category = BacklogCategory.Feature, Percentage = 50m }
                // Only 50%, not 100%
            ]
        };

        var result = await _controller.SetupCycle(cycle.Id, badDto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 16. PUT /api/cycles/{id}/open
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task OpenPlanning_FromSetupWithMembers_Returns200()
    {
        var cycle  = MakeCycle(CycleStatus.Setup);
        var member = ActiveMember();
        cycle.CycleMembers.Add(new CycleMember { TeamMemberId = member.Id, TeamMember = member });
        cycle.CategoryBudgets.Add(new CategoryBudget { Category = BacklogCategory.Feature, Percentage = 100m });

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _cycleRepo.Setup(r => r.UpdateAsync(It.IsAny<PlanningCycle>())).ReturnsAsync(cycle);

        var result = await _controller.OpenPlanning(cycle.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(CycleStatus.Planning, cycle.Status);
    }

    [Fact]
    public async Task OpenPlanning_NotInSetupState_Returns400()
    {
        var cycle = MakeCycle(CycleStatus.Planning); // already open
        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);

        var result = await _controller.OpenPlanning(cycle.Id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 17. GET /api/cycles/active
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetActiveCycle_Exists_Returns200()
    {
        var cycle = MakeCycle(CycleStatus.Planning);
        _cycleRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(cycle);

        var result = await _controller.GetActiveCycle();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetActiveCycle_NoActive_Returns404()
    {
        _cycleRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((PlanningCycle?)null);

        var result = await _controller.GetActiveCycle();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 18. PUT /api/cycles/{id}/freeze
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task FreezePlan_ValidPlanningCycle_Returns200()
    {
        var cycle  = MakeCycle(CycleStatus.Planning);
        var member = ActiveMember();
        cycle.CycleMembers.Add(new CycleMember { TeamMemberId = member.Id, TeamMember = member });
        cycle.CategoryBudgets.Add(new CategoryBudget { Category = BacklogCategory.Feature, Percentage = 100m });

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _cycleRepo.Setup(r => r.UpdateAsync(It.IsAny<PlanningCycle>())).ReturnsAsync(cycle);

        var result = await _controller.FreezePlan(cycle.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(CycleStatus.Frozen, cycle.Status);
    }

    // ─────────────────────────────────────────────────────────────
    // 19. PUT /api/cycles/{id}/complete
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task FinishWeek_FromFrozen_Returns200()
    {
        var cycle = MakeCycle(CycleStatus.Frozen);
        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id)).ReturnsAsync(cycle);
        _cycleRepo.Setup(r => r.UpdateAsync(It.IsAny<PlanningCycle>())).ReturnsAsync(cycle);

        var result = await _controller.FinishWeek(cycle.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(CycleStatus.Completed, cycle.Status);
    }

    // ─────────────────────────────────────────────────────────────
    // 20. DELETE /api/cycles/{id}
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CancelCycle_ActiveCycle_Returns200WithCancelledStatus()
    {
        var cycle     = MakeCycle(CycleStatus.Planning);
        var cancelled = MakeCycle(CycleStatus.Cancelled);
        cancelled.Id  = cycle.Id;

        _cycleRepo.Setup(r => r.GetByIdAsync(cycle.Id))
                  .ReturnsAsync(cycle);
        _cycleRepo.Setup(r => r.DeleteAsync(cycle.Id))
                  .Returns(Task.CompletedTask);
        // Second call after cancel
        _cycleRepo.SetupSequence(r => r.GetByIdAsync(cycle.Id))
                  .ReturnsAsync(cycle)
                  .ReturnsAsync(cancelled);

        var result = await _controller.CancelCycle(cycle.Id);

        Assert.IsType<OkObjectResult>(result);
    }
}

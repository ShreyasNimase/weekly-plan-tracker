using Microsoft.AspNetCore.Mvc;
using Moq;
using WeeklyPlanner.API.Controllers;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.Tests;

public class TeamMembersControllerTests
{
    private readonly Mock<ITeamMemberRepository> _repoMock;
    private readonly TeamMembersController _controller;

    public TeamMembersControllerTests()
    {
        _repoMock = new Mock<ITeamMemberRepository>();
        _controller = new TeamMembersController(_repoMock.Object);
    }

    // ─────────────────────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────────────────────
    private static TeamMember MakeMember(string name = "Alice", bool isLead = false, bool isActive = true)
        => new() { Id = Guid.NewGuid(), Name = name, IsLead = isLead, IsActive = isActive };

    // ─────────────────────────────────────────────────────────────
    // 1. POST /api/team-members — Create Member
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateMember_ValidDto_Returns201()
    {
        var dto = new CreateTeamMemberDto { Name = "Alice", IsLead = false };
        var saved = MakeMember("Alice");

        _repoMock.Setup(r => r.AddAsync(It.IsAny<TeamMember>()))
                 .ReturnsAsync(saved);

        // Simulate that GetMemberById would resolve (needed for CreatedAtAction)
        _repoMock.Setup(r => r.GetByIdAsync(saved.Id))
                 .ReturnsAsync(saved);

        var result = await _controller.CreateMember(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task CreateMember_EmptyName_Returns400()
    {
        // Simulate ASP.NET model validation failure
        _controller.ModelState.AddModelError("Name", "Name is required.");

        var result = await _controller.CreateMember(new CreateTeamMemberDto { Name = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 2. GET /api/team-members — Get All Active
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetAllActive_Returns200WithList()
    {
        var members = new List<TeamMember> { MakeMember("Alice"), MakeMember("Bob") };
        _repoMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(members);

        var result = await _controller.GetAllActiveMembers();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────
    // 3. GET /api/team-members/{id} — Get By Id
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetById_Found_Returns200()
    {
        var member = MakeMember("Alice");
        _repoMock.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);

        var result = await _controller.GetMemberById(member.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TeamMember?)null);

        var result = await _controller.GetMemberById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────────
    // 4. PUT /api/team-members/{id} — Update Name
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateName_Found_Returns200WithNewName()
    {
        var member = MakeMember("Alice");
        _repoMock.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<TeamMember>())).ReturnsAsync(member);

        var result = await _controller.UpdateMemberName(member.Id, new UpdateTeamMemberDto { Name = "Alice Updated" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────
    // 5. PUT /api/team-members/{id}/make-lead — Make Lead
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task MakeLead_ActiveMember_Returns200WithIsLeadTrue()
    {
        var member = MakeMember("Alice", isLead: false);
        _repoMock.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _repoMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<TeamMember> { member });
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<TeamMember>())).ReturnsAsync(member);

        var result = await _controller.MakeLead(member.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.True(member.IsLead); // Mutated in place
    }

    // ─────────────────────────────────────────────────────────────
    // 6. PUT /api/team-members/{id}/deactivate — Deactivate
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Deactivate_NotInCycle_Returns200WithIsActiveFalse()
    {
        var member = MakeMember("Alice");
        _repoMock.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _repoMock.Setup(r => r.IsInActiveCycleAsync(member.Id)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<TeamMember>())).ReturnsAsync(member);

        var result = await _controller.DeactivateMember(member.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.False(member.IsActive); // Mutated in place
    }

    [Fact]
    public async Task Deactivate_InActiveCycle_Returns400()
    {
        var member = MakeMember("Alice");
        _repoMock.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _repoMock.Setup(r => r.IsInActiveCycleAsync(member.Id)).ReturnsAsync(true); // ← business rule violation

        var result = await _controller.DeactivateMember(member.Id);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────
    // 7. PUT /api/team-members/{id}/reactivate — Reactivate
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Reactivate_InactiveMember_Returns200WithIsActiveTrue()
    {
        var member = MakeMember("Alice", isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<TeamMember>())).ReturnsAsync(member);

        var result = await _controller.ReactivateMember(member.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.True(member.IsActive); // Mutated in place
    }
}

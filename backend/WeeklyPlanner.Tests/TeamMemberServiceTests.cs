using Microsoft.EntityFrameworkCore;
using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class TeamMemberServiceTests
{
    private readonly Mock<ITeamMemberRepository> _repo;
    private readonly TeamMemberService _service;

    public TeamMemberServiceTests()
    {
        _repo = new Mock<ITeamMemberRepository>();

        // TeamMemberService(repo, context) — context is used only in MakeLeadAsync.
        // Use an isolated in-memory database so the constructor is satisfied.
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(dbOptions);

        _service = new TeamMemberService(_repo.Object, context);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsError()
    {
        var (result, error) = await _service.CreateAsync(new CreateTeamMemberRequest { Name = "   " });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_NameTooLong_ReturnsError()
    {
        var (result, error) = await _service.CreateAsync(new CreateTeamMemberRequest { Name = new string('x', 101) });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("100", error);
    }

    [Fact]
    public async Task Create_DuplicateNameAmongActive_ReturnsError()
    {
        var existing = new TeamMember { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };
        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { existing });
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { existing });

        var (result, error) = await _service.CreateAsync(new CreateTeamMemberRequest { Name = "alice" });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("already has this name", error);
    }

    [Fact]
    public async Task Create_FirstMember_SetsIsLeadTrue()
    {
        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TeamMember>());
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TeamMember>());
        TeamMember? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<TeamMember>(), It.IsAny<CancellationToken>()))
            .Callback<TeamMember, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync((TeamMember m, CancellationToken _) => m);

        var (result, error) = await _service.CreateAsync(new CreateTeamMemberRequest { Name = "Alice" });
        Assert.NotNull(result);
        Assert.True(result.IsLead);
        Assert.Null(error);
        Assert.NotNull(captured);
        Assert.True(captured.IsLead);
    }

    [Fact]
    public async Task Create_NotFirstMember_SetsIsLeadFalse()
    {
        var existing = new TeamMember { Id = Guid.NewGuid(), Name = "Bob", IsActive = true };
        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { existing });
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { existing });
        TeamMember? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<TeamMember>(), It.IsAny<CancellationToken>()))
            .Callback<TeamMember, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync((TeamMember m, CancellationToken _) => m);

        var (result, error) = await _service.CreateAsync(new CreateTeamMemberRequest { Name = "Alice" });
        Assert.NotNull(result);
        Assert.False(result.IsLead);
        Assert.Null(error);
        Assert.NotNull(captured);
        Assert.False(captured.IsLead);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);
        var id = Guid.NewGuid();
        var (result, error) = await _service.UpdateAsync(id, new UpdateTeamMemberRequest { Name = "Alice" });
        Assert.Null(result);
        Assert.Equal("Team member not found.", error);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsError()
    {
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Alice", IsActive = true };
        var other = new TeamMember { Id = Guid.NewGuid(), Name = "Bob", IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { member, other });

        var (result, error) = await _service.UpdateAsync(id, new UpdateTeamMemberRequest { Name = "bob" });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("already has this name", error);
    }

    [Fact]
    public async Task Update_Success_ReturnsUpdated()
    {
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Alice", IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { member });
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TeamMember>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember m, CancellationToken _) => m);

        var (result, error) = await _service.UpdateAsync(id, new UpdateTeamMemberRequest { Name = "Alice Chen" });
        Assert.NotNull(result);
        Assert.Equal("Alice Chen", result.Name);
        Assert.Null(error);
    }

    [Fact]
    public async Task MakeLead_NotFound_ReturnsError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);
        var (result, error) = await _service.MakeLeadAsync(Guid.NewGuid());
        Assert.Null(result);
        Assert.Equal("Team member not found.", error);
    }

    [Fact]
    public async Task MakeLead_Inactive_ReturnsError()
    {
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Alice", IsActive = false };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        var (result, error) = await _service.MakeLeadAsync(id);
        Assert.Null(result);
        Assert.Contains("inactive", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deactivate_InActiveCycle_ReturnsError()
    {
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Alice", IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _repo.Setup(r => r.IsInActiveCycleAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var (result, error) = await _service.DeactivateAsync(id);
        Assert.Null(result);
        Assert.Contains("active plan", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deactivate_Success_ReturnsUpdated()
    {
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Alice", IsActive = true, IsLead = true };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _repo.Setup(r => r.IsInActiveCycleAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TeamMember>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember m, CancellationToken _) => m);

        var (result, error) = await _service.DeactivateAsync(id);
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.False(result.IsLead);
        Assert.Null(error);
    }

    [Fact]
    public async Task Reactivate_AlreadyActive_ReturnsError()
    {
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Alice", IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        var (result, error) = await _service.ReactivateAsync(id);
        Assert.Null(result);
        Assert.Contains("already active", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllSortedByCreatedAt()
    {
        var members = new[]
        {
            new TeamMember { Id = Guid.NewGuid(), Name = "A", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new TeamMember { Id = Guid.NewGuid(), Name = "B", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(members);
        var result = await _service.GetAllAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("B", result[1].Name);
    }
}

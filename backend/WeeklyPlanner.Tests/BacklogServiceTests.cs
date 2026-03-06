using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class BacklogServiceTests
{
    private readonly Mock<IBacklogRepository> _repo;
    private readonly BacklogService _service;

    public BacklogServiceTests()
    {
        _repo = new Mock<IBacklogRepository>();
        _service = new BacklogService(_repo.Object);
    }

    [Fact]
    public async Task Create_EmptyTitle_ReturnsError()
    {
        var (result, error) = await _service.CreateAsync(new CreateBacklogItemRequest { Title = "   ", Category = "CLIENT_FOCUSED" });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("Title", error);
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsError()
    {
        var (result, error) = await _service.CreateAsync(new CreateBacklogItemRequest { Title = "Task", Category = "INVALID" });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("Category", error);
    }

    [Fact]
    public async Task Create_EstimatedEffortNotHalfStep_ReturnsError()
    {
        var (result, error) = await _service.CreateAsync(new CreateBacklogItemRequest
        {
            Title = "Task",
            Category = "CLIENT_FOCUSED",
            EstimatedEffort = 1.3m
        });
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("0.5", error);
    }

    [Fact]
    public async Task Create_EstimatedEffortOutOfRange_ReturnsError()
    {
        var (result, error) = await _service.CreateAsync(new CreateBacklogItemRequest
        {
            Title = "Task",
            Category = "CLIENT_FOCUSED",
            EstimatedEffort = 1000m
        });
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Create_Success_StatusAvailable()
    {
        BacklogItem? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<BacklogItem>(), It.IsAny<CancellationToken>()))
            .Callback<BacklogItem, CancellationToken>((i, _) => captured = i)
            .ReturnsAsync((BacklogItem i, CancellationToken _) => i);

        var (result, error) = await _service.CreateAsync(new CreateBacklogItemRequest
        {
            Title = "Task",
            Category = "TECH_DEBT",
            EstimatedEffort = 2.5m
        });
        Assert.NotNull(result);
        Assert.Equal("AVAILABLE", result.Status);
        Assert.Null(error);
        Assert.NotNull(captured);
        Assert.Equal("AVAILABLE", captured.Status);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((BacklogItem?)null);
        var (result, error) = await _service.UpdateAsync(Guid.NewGuid(), new UpdateBacklogItemRequest { Title = "Task" });
        Assert.Null(result);
        Assert.Equal("Backlog item not found.", error);
    }

    [Fact]
    public async Task Update_DoesNotChangeCategory()
    {
        var id = Guid.NewGuid();
        var item = new BacklogItem { Id = id, Title = "Old", Category = "CLIENT_FOCUSED", Status = "AVAILABLE" };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<BacklogItem>(), It.IsAny<CancellationToken>())).ReturnsAsync((BacklogItem i, CancellationToken _) => i);

        var (result, error) = await _service.UpdateAsync(id, new UpdateBacklogItemRequest { Title = "New", Description = "Desc" });
        Assert.NotNull(result);
        Assert.Equal("CLIENT_FOCUSED", result.Category);
        Assert.Equal("New", result.Title);
        Assert.Null(error);
    }

    [Fact]
    public async Task Archive_WhenInPlan_ReturnsError()
    {
        var id = Guid.NewGuid();
        var item = new BacklogItem { Id = id, Title = "Task", Status = "IN_PLAN" };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var (result, error) = await _service.ArchiveAsync(id);
        Assert.Null(result);
        Assert.Contains("active plan", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Archive_Success_ReturnsArchived()
    {
        var id = Guid.NewGuid();
        var item = new BacklogItem { Id = id, Title = "Task", Status = "AVAILABLE" };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<BacklogItem>(), It.IsAny<CancellationToken>())).ReturnsAsync((BacklogItem i, CancellationToken _) => i);

        var (result, error) = await _service.ArchiveAsync(id);
        Assert.NotNull(result);
        Assert.Equal("ARCHIVED", result.Status);
        Assert.Null(error);
    }

    [Fact]
    public async Task Delete_WhenInPlan_ReturnsError()
    {
        var id = Guid.NewGuid();
        var item = new BacklogItem { Id = id, Title = "Task", Status = "IN_PLAN" };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var (success, error) = await _service.DeleteAsync(id);
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("AVAILABLE", error!);
    }

    [Fact]
    public async Task Delete_WhenAvailable_Success()
    {
        var id = Guid.NewGuid();
        var item = new BacklogItem { Id = id, Title = "Task", Status = "AVAILABLE" };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _repo.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var (success, error) = await _service.DeleteAsync(id);
        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task Delete_WhenArchived_Success()
    {
        var id = Guid.NewGuid();
        var item = new BacklogItem { Id = id, Title = "Task", Status = "ARCHIVED" };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _repo.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var (success, error) = await _service.DeleteAsync(id);
        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((BacklogItem?)null);
        var (success, error) = await _service.DeleteAsync(Guid.NewGuid());
        Assert.False(success);
        Assert.Equal("Backlog item not found.", error);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((BacklogItem?)null);
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }
}

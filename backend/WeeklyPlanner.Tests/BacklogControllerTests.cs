using Microsoft.AspNetCore.Mvc;
using Moq;
using WeeklyPlanner.API.Controllers;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.Tests;

public class BacklogControllerTests
{
    private readonly Mock<IBacklogRepository> _repoMock;
    private readonly BacklogController _controller;

    public BacklogControllerTests()
    {
        _repoMock   = new Mock<IBacklogRepository>();
        _controller = new BacklogController(_repoMock.Object);
    }

    // ─── helpers ──────────────────────────────────────────────
    private static BacklogItem MakeItem(
        string title    = "Do something",
        BacklogStatus s = BacklogStatus.Active,
        BacklogCategory c = BacklogCategory.Feature)
        => new() { Id = Guid.NewGuid(), Title = title, Status = s, Category = c };

    // ─────────────────────────────────────────────────────────
    // 8. POST /api/backlog
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateItem_ValidDto_Returns201()
    {
        var dto  = new CreateBacklogItemDto { Title = "New Feature", Category = BacklogCategory.Feature };
        var item = MakeItem("New Feature");
        _repoMock.Setup(r => r.AddAsync(It.IsAny<BacklogItem>())).ReturnsAsync(item);
        _repoMock.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _controller.CreateItem(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task CreateItem_EmptyTitle_Returns400()
    {
        _controller.ModelState.AddModelError("Title", "Title is required.");

        var result = await _controller.CreateItem(new CreateBacklogItemDto { Title = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────
    // 9. GET /api/backlog (with filters)
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GetAll_NoFilters_Returns200WithList()
    {
        var items = new List<BacklogItem> { MakeItem("A"), MakeItem("B") };
        _repoMock.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(items);

        var result = await _controller.GetAll(null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    // ─────────────────────────────────────────────────────────
    // 10. GET /api/backlog/{id}
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GetById_Found_Returns200()
    {
        var item = MakeItem();
        _repoMock.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _controller.GetById(item.Id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BacklogItem?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────
    // 11. PUT /api/backlog/{id}
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateItem_Found_Returns200()
    {
        var item = MakeItem();
        _repoMock.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<BacklogItem>())).ReturnsAsync(item);

        var dto    = new UpdateBacklogItemDto { Title = "Updated Title" };
        var result = await _controller.UpdateItem(item.Id, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_NotFound_Returns404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BacklogItem?)null);

        var result = await _controller.UpdateItem(Guid.NewGuid(), new UpdateBacklogItemDto { Title = "X" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─────────────────────────────────────────────────────────
    // 12. PUT /api/backlog/{id}/archive
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task ArchiveItem_ActiveItem_Returns200WithArchivedStatus()
    {
        var item = MakeItem();
        _repoMock.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<BacklogItem>())).ReturnsAsync(item);

        var result = await _controller.ArchiveItem(item.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(BacklogStatus.Archived, item.Status); // mutated in-place
    }

    [Fact]
    public async Task ArchiveItem_AlreadyArchived_Returns400()
    {
        var item = MakeItem(s: BacklogStatus.Archived);
        _repoMock.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _controller.ArchiveItem(item.Id);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    // ─────────────────────────────────────────────────────────
    // 13. DELETE /api/backlog/{id}
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task DeleteItem_Found_Returns204()
    {
        var item = MakeItem();
        _repoMock.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _repoMock.Setup(r => r.DeleteAsync(item.Id)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteItem(item.Id);

        Assert.IsType<NoContentResult>(result);
    }
}

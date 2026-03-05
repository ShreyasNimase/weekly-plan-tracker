using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/backlog")]
public class BacklogController : ControllerBase
{
    private readonly IBacklogRepository _repository;

    public BacklogController(IBacklogRepository repository)
    {
        _repository = repository;
    }

    // ─────────────────────────────────────────────
    // 8. POST /api/backlog — Create Backlog Item
    // ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] CreateBacklogItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var item = new BacklogItem
        {
            Title          = dto.Title.Trim(),
            Description    = dto.Description?.Trim(),
            Category       = dto.Category,
            Priority       = dto.Priority,
            EstimatedHours = dto.EstimatedHours,
            Status         = BacklogStatus.Active,
            CreatedAt      = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
    }

    // ─────────────────────────────────────────────
    // 9. GET /api/backlog?category=&status=&search=
    // ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BacklogCategory? category,
        [FromQuery] BacklogStatus?   status,
        [FromQuery] string?          search)
    {
        var items = await _repository.GetAllAsync(category, status, search);
        return Ok(items.Select(ToResponse));
    }

    // ─────────────────────────────────────────────
    // 10. GET /api/backlog/{id} — Get By Id
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { message = $"Backlog item '{id}' not found." });

        return Ok(ToResponse(item));
    }

    // ─────────────────────────────────────────────
    // 11. PUT /api/backlog/{id} — Update Backlog Item
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateBacklogItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { message = $"Backlog item '{id}' not found." });

        if (item.Status == BacklogStatus.Archived)
            return BadRequest(new { message = "Cannot update an archived backlog item. Restore it first." });

        item.Title          = dto.Title.Trim();
        item.Description    = dto.Description?.Trim();
        item.Category       = dto.Category;
        item.Priority       = dto.Priority;
        item.EstimatedHours = dto.EstimatedHours;

        await _repository.UpdateAsync(item);
        return Ok(ToResponse(item));
    }

    // ─────────────────────────────────────────────
    // 12. PUT /api/backlog/{id}/archive — Archive
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveItem(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { message = $"Backlog item '{id}' not found." });

        if (item.Status == BacklogStatus.Archived)
            return BadRequest(new { message = "Backlog item is already archived." });

        item.Status = BacklogStatus.Archived;
        await _repository.UpdateAsync(item);
        return Ok(ToResponse(item));
    }

    // ─────────────────────────────────────────────
    // 13. DELETE /api/backlog/{id} — Hard Delete
    // ─────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { message = $"Backlog item '{id}' not found." });

        await _repository.DeleteAsync(id);
        return NoContent(); // 204
    }

    // ─────────────────────────────────────────────
    // Shared response shaping
    // ─────────────────────────────────────────────
    private static object ToResponse(BacklogItem i) => new
    {
        i.Id,
        i.Title,
        i.Description,
        Category       = i.Category.ToString(),
        Status         = i.Status.ToString(),
        Priority       = i.Priority.ToString(),
        i.EstimatedHours,
        i.CreatedAt
    };
}

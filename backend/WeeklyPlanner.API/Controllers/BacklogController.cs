using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>APIs for managing the backlog.</summary>
[ApiController]
[Route("api/backlog")]
public class BacklogController : ControllerBase
{
    private readonly IBacklogService _service;

    public BacklogController(IBacklogService service)
    {
        _service = service;
    }

    /// <summary>Creates a new backlog item. Request body: { title, description?, category, estimatedEffort? }. Status = AVAILABLE.</summary>
    /// <response code="201">Returns the created item.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BacklogItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBacklogItemRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrors());

        var (result, error) = await _service.CreateAsync(request, cancellationToken);
        if (error != null)
            return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>Gets backlog items. Query: category?, status? (default AVAILABLE+IN_PLAN, ARCHIVED, COMPLETED, ALL), search? (title). Sorted by CreatedAt descending.</summary>
    /// <response code="200">Returns the list of items.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BacklogItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var list = await _service.GetAllAsync(category, status, search, cancellationToken);
        return Ok(list);
    }

    /// <summary>Gets a single backlog item by id.</summary>
    /// <response code="200">Returns the item.</response>
    /// <response code="404">Item not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BacklogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound(new { message = "Backlog item not found." });
        return Ok(item);
    }

    /// <summary>Updates a backlog item. Request body: { title, description?, estimatedEffort? }. Category is not updatable.</summary>
    /// <response code="200">Returns the updated item.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Item not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BacklogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBacklogItemRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrors());

        var (result, error) = await _service.UpdateAsync(id, request, cancellationToken);
        if (error == "Backlog item not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Archives the item. Fails if status is IN_PLAN (part of an active plan).</summary>
    /// <response code="200">Returns the updated item.</response>
    /// <response code="400">Item is part of an active plan.</response>
    /// <response code="404">Item not found.</response>
    [HttpPut("{id:guid}/archive")]
    [ProducesResponseType(typeof(BacklogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.ArchiveAsync(id, cancellationToken);
        if (error == "Backlog item not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Deletes the item. Only allowed when status is AVAILABLE or ARCHIVED.</summary>
    /// <response code="204">Item deleted.</response>
    /// <response code="400">Item cannot be deleted (wrong status).</response>
    /// <response code="404">Item not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (error == "Backlog item not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return NoContent();
    }

    private IActionResult ValidationErrors()
    {
        var errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
        return BadRequest(new { errors });
    }
}

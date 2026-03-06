using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>APIs for planning cycles. Only one active cycle (SETUP, PLANNING, FROZEN) at a time.</summary>
[ApiController]
[Route("api/cycles")]
public class CyclesController : ControllerBase
{
    private readonly ICycleService _service;

    public CyclesController(ICycleService service)
    {
        _service = service;
    }

    /// <summary>Starts a new cycle (next Tuesday). Fails if there is already a week being planned.</summary>
    /// <response code="201">Cycle created.</response>
    /// <response code="400">Active cycle already exists.</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(CycleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.StartAsync(cancellationToken);
        if (error != null)
            return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetActive), null, result);
    }

    /// <summary>Setup: planningDate (Tuesday), memberIds, categoryAllocations (exactly 3, sum 100). Replaces existing setup.</summary>
    /// <response code="200">Setup applied.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Cycle not found.</response>
    [HttpPut("{id:guid}/setup")]
    [ProducesResponseType(typeof(CycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Setup(Guid id, [FromBody] SetupCycleRequest request, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.SetupAsync(id, request, cancellationToken);
        if (error == "Cycle not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(new { success = true, message = "Cycle setup saved.", data = result });
    }

    /// <summary>Opens planning (SETUP → PLANNING).</summary>
    /// <response code="200">Cycle in PLANNING.</response>
    /// <response code="400">Invalid state or no members/allocations.</response>
    /// <response code="404">Cycle not found.</response>
    [HttpPut("{id:guid}/open")]
    [ProducesResponseType(typeof(CycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Open(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.OpenAsync(id, cancellationToken);
        if (error == "Cycle not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(new { success = true, message = "Planning is open!", data = result });
    }

    /// <summary>Gets the active cycle (state SETUP, PLANNING, or FROZEN). Returns 204 if none.</summary>
    /// <response code="200">Active cycle with CategoryAllocations, CycleMembers, MemberPlans.</response>
    /// <response code="204">No active cycle.</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(CycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken = default)
    {
        var cycle = await _service.GetActiveAsync(cancellationToken);
        if (cycle is null)
            return NoContent();
        return Ok(cycle);
    }

    /// <summary>Freezes the plan (PLANNING → FROZEN). Validates each member 30h and each category budget.</summary>
    /// <response code="200">Cycle frozen.</response>
    /// <response code="400">Validation errors (member hours or category budget).</response>
    /// <response code="404">Cycle not found.</response>
    [HttpPut("{id:guid}/freeze")]
    [ProducesResponseType(typeof(CycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Freeze(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, errors) = await _service.FreezeAsync(id, cancellationToken);
        if (errors?.Count > 0)
        {
            if (errors.Count == 1 && errors[0] == "Cycle not found.")
                return NotFound(new { message = errors[0] });
            return BadRequest(new { errors });
        }
        if (result is null)
            return BadRequest();
        return Ok(result);
    }

    /// <summary>Completes the cycle (FROZEN → COMPLETED). Updates BacklogItem statuses.</summary>
    /// <response code="200">Cycle completed.</response>
    /// <response code="400">Invalid state.</response>
    /// <response code="404">Cycle not found.</response>
    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType(typeof(CycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.CompleteAsync(id, cancellationToken);
        if (error == "Cycle not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Deletes the cycle (only SETUP or PLANNING). Resets affected BacklogItems to AVAILABLE if needed.</summary>
    /// <response code="204">Cycle deleted.</response>
    /// <response code="400">Cannot delete FROZEN/COMPLETED.</response>
    /// <response code="404">Cycle not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var error = await _service.DeleteAsync(id, cancellationToken);
        if (error == "Cycle not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return NoContent();
    }

    /// <summary>History: cycles with state FROZEN or COMPLETED, sorted by PlanningDate descending.</summary>
    /// <response code="200">List with member count.</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<CycleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken = default)
    {
        var list = await _service.GetHistoryAsync(cancellationToken);
        return Ok(list);
    }
}

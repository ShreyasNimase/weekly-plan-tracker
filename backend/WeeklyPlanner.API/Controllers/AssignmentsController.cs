using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>APIs for task assignments (backlog item committed to a member plan).</summary>
[ApiController]
[Route("api/assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _service;

    public AssignmentsController(IAssignmentService service)
    {
        _service = service;
    }

    /// <summary>Creates an assignment. Validates 30h cap and category budget. Sets BacklogItem IN_PLAN, updates MemberPlan.</summary>
    /// <response code="201">Assignment created.</response>
    /// <response code="400">Validation or guard failed.</response>
    /// <response code="404">Member plan or backlog item not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.CreateAsync(request, cancellationToken);
        if (error == "Member plan not found." || error == "Backlog item not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>Updates committed hours. Validates 30h cap and category budget. Updates MemberPlan, sets IsReady=false.</summary>
    /// <response code="200">Assignment updated.</response>
    /// <response code="400">Validation or guard failed.</response>
    /// <response code="404">Assignment not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.UpdateAsync(id, request, cancellationToken);
        if (error == "Assignment not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Deletes an assignment. Sets BacklogItem AVAILABLE if no other in cycle. Updates MemberPlan.</summary>
    /// <response code="204">Assignment deleted.</response>
    /// <response code="400">Cycle not in PLANNING.</response>
    /// <response code="404">Assignment not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var error = await _service.DeleteAsync(id, cancellationToken);
        if (error == "Assignment not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return NoContent();
    }

    /// <summary>Gets an assignment by id.</summary>
    /// <response code="200">Assignment.</response>
    /// <response code="404">Not found.</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var a = await _service.GetByIdAsync(id, cancellationToken);
        if (a is null)
            return NotFound(new { message = "Assignment not found." });
        return Ok(a);
    }
}

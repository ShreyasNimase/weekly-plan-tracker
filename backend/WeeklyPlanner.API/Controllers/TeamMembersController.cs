using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>APIs for managing team members.</summary>
[ApiController]
[Route("api/team-members")]
public class TeamMembersController : ControllerBase
{
    private readonly ITeamMemberService _service;

    public TeamMembersController(ITeamMemberService service)
    {
        _service = service;
    }

    /// <summary>Creates a new team member. First member ever becomes lead automatically. Request body: { name }.</summary>
    /// <response code="201">Returns the created member.</response>
    /// <response code="400">Validation failed or duplicate name among active members.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrors());

        var (result, error) = await _service.CreateAsync(request, cancellationToken);
        if (error != null)
            return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>Gets all team members (active and inactive), sorted by CreatedAt ascending.</summary>
    /// <response code="200">Returns the list of members.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var list = await _service.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    /// <summary>Gets a single team member by id.</summary>
    /// <response code="200">Returns the member.</response>
    /// <response code="404">Member not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _service.GetByIdAsync(id, cancellationToken);
        if (member is null)
            return NotFound(new { message = "Team member not found." });
        return Ok(member);
    }

    /// <summary>Updates a team member's name. Request body: { name }.</summary>
    /// <response code="200">Returns the updated member.</response>
    /// <response code="400">Validation failed or another member has the same name.</response>
    /// <response code="404">Member not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrors());

        var (result, error) = await _service.UpdateAsync(id, request, cancellationToken);
        if (error == "Team member not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Makes this member the lead and sets all others to non-lead.</summary>
    /// <response code="200">Returns the updated member.</response>
    /// <response code="400">Member is inactive.</response>
    /// <response code="404">Member not found.</response>
    [HttpPut("{id:guid}/make-lead")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MakeLead(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.MakeLeadAsync(id, cancellationToken);
        if (error == "Team member not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Deactivates the member. Fails if the member is part of a cycle in SETUP, PLANNING, or FROZEN.</summary>
    /// <response code="200">Returns the updated member.</response>
    /// <response code="400">Member already inactive or part of an active plan.</response>
    /// <response code="404">Member not found.</response>
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.DeactivateAsync(id, cancellationToken);
        if (error == "Team member not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Reactivates an inactive member.</summary>
    /// <response code="200">Returns the updated member.</response>
    /// <response code="400">Member already active.</response>
    /// <response code="404">Member not found.</response>
    [HttpPut("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var (result, error) = await _service.ReactivateAsync(id, cancellationToken);
        if (error == "Team member not found.")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });
        return Ok(result);
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

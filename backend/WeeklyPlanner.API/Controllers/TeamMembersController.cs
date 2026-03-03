using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api/team-members")]
public class TeamMembersController : ControllerBase
{
    private readonly ITeamMemberRepository _repository;

    public TeamMembersController(ITeamMemberRepository repository)
    {
        _repository = repository;
    }

    // POST /api/team-members
    [HttpPost]
    public async Task<IActionResult> AddMember([FromBody] CreateTeamMemberDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var member = new TeamMember
        {
            Name = dto.Name,
            IsLead = dto.IsLead,
            IsActive = true   // always default to active on creation
        };

        var created = await _repository.AddAsync(member);

        return CreatedAtAction(nameof(AddMember), new { id = created.Id }, new
        {
            created.Id,
            created.Name,
            created.IsLead,
            created.IsActive
        });
    }
}

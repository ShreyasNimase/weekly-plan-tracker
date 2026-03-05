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

    // ─────────────────────────────────────────────
    // 1. POST /api/team-members — Create Member
    // ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateMember([FromBody] CreateTeamMemberDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var member = new TeamMember
        {
            Name = dto.Name.Trim(),
            IsLead = dto.IsLead,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(member);

        return CreatedAtAction(nameof(GetMemberById), new { id = created.Id }, ToResponse(created));
    }

    // ─────────────────────────────────────────────
    // 2. GET /api/team-members — Get All Active
    // ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAllActiveMembers()
    {
        var members = await _repository.GetAllActiveAsync();
        return Ok(members.Select(ToResponse));
    }

    // ─────────────────────────────────────────────
    // 3. GET /api/team-members/{id} — Get By Id
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMemberById(Guid id)
    {
        var member = await _repository.GetByIdAsync(id);
        if (member is null)
            return NotFound(new { message = $"Team member '{id}' not found." });

        return Ok(ToResponse(member));
    }

    // ─────────────────────────────────────────────
    // 4. PUT /api/team-members/{id} — Update Name
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMemberName(Guid id, [FromBody] UpdateTeamMemberDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var member = await _repository.GetByIdAsync(id);
        if (member is null)
            return NotFound(new { message = $"Team member '{id}' not found." });

        member.Name = dto.Name.Trim();
        await _repository.UpdateAsync(member);

        return Ok(ToResponse(member));
    }

    // ─────────────────────────────────────────────
    // 5. PUT /api/team-members/{id}/make-lead — Make Lead
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/make-lead")]
    public async Task<IActionResult> MakeLead(Guid id)
    {
        var member = await _repository.GetByIdAsync(id);
        if (member is null)
            return NotFound(new { message = $"Team member '{id}' not found." });

        if (!member.IsActive)
            return BadRequest(new { message = "Cannot make an inactive member the lead." });

        // De-promote all other leads first
        var allActive = await _repository.GetAllActiveAsync();
        foreach (var m in allActive.Where(m => m.IsLead && m.Id != id))
        {
            m.IsLead = false;
            await _repository.UpdateAsync(m);
        }

        member.IsLead = true;
        await _repository.UpdateAsync(member);

        return Ok(ToResponse(member));
    }

    // ─────────────────────────────────────────────
    // 6. PUT /api/team-members/{id}/deactivate — Deactivate
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateMember(Guid id)
    {
        var member = await _repository.GetByIdAsync(id);
        if (member is null)
            return NotFound(new { message = $"Team member '{id}' not found." });

        if (!member.IsActive)
            return BadRequest(new { message = "Member is already inactive." });

        // ⚠ Business rule: Cannot deactivate a member in an active cycle
        var inActiveCycle = await _repository.IsInActiveCycleAsync(id);
        if (inActiveCycle)
            return BadRequest(new { message = "Cannot deactivate a member who is part of an active planning cycle." });

        member.IsActive = false;
        member.IsLead = false; // leads cannot stay lead when deactivated
        await _repository.UpdateAsync(member);

        return Ok(ToResponse(member));
    }

    // ─────────────────────────────────────────────
    // 7. PUT /api/team-members/{id}/reactivate — Reactivate
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateMember(Guid id)
    {
        var member = await _repository.GetByIdAsync(id);
        if (member is null)
            return NotFound(new { message = $"Team member '{id}' not found." });

        if (member.IsActive)
            return BadRequest(new { message = "Member is already active." });

        member.IsActive = true;
        await _repository.UpdateAsync(member);

        return Ok(ToResponse(member));
    }

    // ─────────────────────────────────────────────
    // Shared response shaping
    // ─────────────────────────────────────────────
    private static object ToResponse(TeamMember m) => new
    {
        m.Id,
        m.Name,
        m.IsLead,
        m.IsActive,
        m.CreatedAt
    };
}


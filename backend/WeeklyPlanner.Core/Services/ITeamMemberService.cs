using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for team member operations.</summary>
public interface ITeamMemberService
{
    /// <summary>Creates a new team member. First member ever becomes lead.</summary>
    Task<(TeamMemberDto? Result, string? Error)> CreateAsync(CreateTeamMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets all members (active and inactive) sorted by CreatedAt ascending.</summary>
    Task<IReadOnlyList<TeamMemberDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a single member by id.</summary>
    Task<TeamMemberDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates member name. Returns error if another member has the same name.</summary>
    Task<(TeamMemberDto? Result, string? Error)> UpdateAsync(Guid id, UpdateTeamMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets this member as lead and all others as non-lead.</summary>
    Task<(TeamMemberDto? Result, string? Error)> MakeLeadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deactivates member. Fails if member is in an active cycle (SETUP, PLANNING, FROZEN).</summary>
    Task<(TeamMemberDto? Result, string? Error)> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reactivates a member.</summary>
    Task<(TeamMemberDto? Result, string? Error)> ReactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

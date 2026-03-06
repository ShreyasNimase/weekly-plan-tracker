using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Repository for team members.
/// </summary>
public interface ITeamMemberRepository
{
    /// <summary>Adds a new team member.</summary>
    Task<TeamMember> AddAsync(TeamMember teamMember, CancellationToken cancellationToken = default);

    /// <summary>Gets all active team members ordered by CreatedAt.</summary>
    Task<IEnumerable<TeamMember>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets all team members ordered by CreatedAt.</summary>
    Task<IEnumerable<TeamMember>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a team member by id.</summary>
    Task<TeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing team member.</summary>
    Task<TeamMember> UpdateAsync(TeamMember teamMember, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the member is in any cycle whose State is not COMPLETED.</summary>
    Task<bool> IsInActiveCycleAsync(Guid id, CancellationToken cancellationToken = default);
}

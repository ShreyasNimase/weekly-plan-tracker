using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

public interface ITeamMemberRepository
{
    Task<TeamMember> AddAsync(TeamMember teamMember);
    Task<IEnumerable<TeamMember>> GetAllActiveAsync();
    Task<IEnumerable<TeamMember>> GetAllAsync();
    Task<TeamMember?> GetByIdAsync(Guid id);
    Task<TeamMember> UpdateAsync(TeamMember teamMember);
    Task<bool> IsInActiveCycleAsync(Guid id);
}

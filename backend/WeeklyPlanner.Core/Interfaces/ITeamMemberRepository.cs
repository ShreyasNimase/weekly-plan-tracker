using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

public interface ITeamMemberRepository
{
    Task<TeamMember> AddAsync(TeamMember teamMember);
}

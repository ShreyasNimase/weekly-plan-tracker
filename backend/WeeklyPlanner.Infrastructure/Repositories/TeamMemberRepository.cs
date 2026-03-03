using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

public class TeamMemberRepository : ITeamMemberRepository
{
    private readonly AppDbContext _context;

    public TeamMemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TeamMember> AddAsync(TeamMember teamMember)
    {
        _context.TeamMembers.Add(teamMember);
        await _context.SaveChangesAsync();
        return teamMember;
    }
}

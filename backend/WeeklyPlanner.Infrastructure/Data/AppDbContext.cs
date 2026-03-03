using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
}

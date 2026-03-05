using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<BacklogItem> BacklogItems => Set<BacklogItem>();
    public DbSet<PlanningCycle> PlanningCycles => Set<PlanningCycle>();
    public DbSet<CycleMember> CycleMembers => Set<CycleMember>();
    public DbSet<CategoryBudget> CategoryBudgets => Set<CategoryBudget>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BacklogItem decimal precision
        modelBuilder.Entity<BacklogItem>()
            .Property(b => b.EstimatedHours)
            .HasPrecision(18, 2);

        // CycleMember → PlanningCycle (DeleteBehavior.Cascade)
        modelBuilder.Entity<CycleMember>()
            .HasOne(cm => cm.Cycle)
            .WithMany(c => c.CycleMembers)
            .HasForeignKey(cm => cm.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        // CycleMember → TeamMember (no cascade — prevent accidental member deletion)
        modelBuilder.Entity<CycleMember>()
            .HasOne(cm => cm.TeamMember)
            .WithMany()
            .HasForeignKey(cm => cm.TeamMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique: one TeamMember per cycle
        modelBuilder.Entity<CycleMember>()
            .HasIndex(cm => new { cm.CycleId, cm.TeamMemberId })
            .IsUnique();

        // CategoryBudget → PlanningCycle
        modelBuilder.Entity<CategoryBudget>()
            .HasOne(cb => cb.Cycle)
            .WithMany(c => c.CategoryBudgets)
            .HasForeignKey(cb => cb.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Decimal precisions
        modelBuilder.Entity<CycleMember>()
            .Property(cm => cm.AllocatedHours)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CategoryBudget>()
            .Property(cb => cb.Percentage)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CategoryBudget>()
            .Property(cb => cb.HoursBudget)
            .HasPrecision(18, 2);

        // TaskAssignment → CycleMember
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(a => a.CycleMember)
            .WithMany(cm => cm.TaskAssignments)
            .HasForeignKey(a => a.CycleMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // TaskAssignment → BacklogItem (Restrict — don't delete tasks when backlog is deleted)
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(a => a.BacklogItem)
            .WithMany()
            .HasForeignKey(a => a.BacklogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // One BacklogItem per CycleMember per cycle
        modelBuilder.Entity<TaskAssignment>()
            .HasIndex(a => new { a.CycleMemberId, a.BacklogItemId })
            .IsUnique();

        modelBuilder.Entity<TaskAssignment>()
            .Property(a => a.PlannedHours)
            .HasPrecision(18, 2);
    }
}



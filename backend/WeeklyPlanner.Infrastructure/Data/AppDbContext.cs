using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the Weekly Planner application.
/// Configured for Azure SQL Database with retry logic for transient failures.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<BacklogItem> BacklogItems => Set<BacklogItem>();
    public DbSet<PlanningCycle> PlanningCycles => Set<PlanningCycle>();
    public DbSet<CycleMember> CycleMembers => Set<CycleMember>();
    public DbSet<CategoryAllocation> CategoryAllocations => Set<CategoryAllocation>();
    public DbSet<MemberPlan> MemberPlans => Set<MemberPlan>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- TeamMember -----
        modelBuilder.Entity<TeamMember>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Name).IsRequired();
        });

        // ----- BacklogItem -----
        modelBuilder.Entity<BacklogItem>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.EstimatedEffort).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.CreatedByMember)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Status, x.Category });
        });

        // ----- PlanningCycle -----
        modelBuilder.Entity<PlanningCycle>(e =>
        {
            e.Property(x => x.State).HasMaxLength(50).IsRequired();
        });

        // ----- CycleMember -----
        modelBuilder.Entity<CycleMember>(e =>
        {
            e.HasOne(x => x.Cycle)
                .WithMany(c => c.CycleMembers)
                .HasForeignKey(x => x.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CycleId, x.MemberId }).IsUnique();
        });

        // ----- CategoryAllocation -----
        modelBuilder.Entity<CategoryAllocation>(e =>
        {
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.BudgetHours).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Cycle)
                .WithMany(c => c.CategoryAllocations)
                .HasForeignKey(x => x.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CycleId, x.Category }).IsUnique();
        });

        // ----- MemberPlan -----
        modelBuilder.Entity<MemberPlan>(e =>
        {
            e.Property(x => x.TotalPlannedHours).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Cycle)
                .WithMany(c => c.MemberPlans)
                .HasForeignKey(x => x.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CycleId, x.MemberId }).IsUnique();
        });

        // ----- TaskAssignment -----
        modelBuilder.Entity<TaskAssignment>(e =>
        {
            e.Property(x => x.CommittedHours).HasColumnType("decimal(18,2)");
            e.Property(x => x.HoursCompleted).HasColumnType("decimal(18,2)");
            e.Property(x => x.ProgressStatus).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.MemberPlan)
                .WithMany(mp => mp.TaskAssignments)
                .HasForeignKey(x => x.MemberPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BacklogItem)
                .WithMany()
                .HasForeignKey(x => x.BacklogItemId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.MemberPlanId);
        });

        // ----- ProgressUpdate -----
        modelBuilder.Entity<ProgressUpdate>(e =>
        {
            e.Property(x => x.PreviousHoursCompleted).HasColumnType("decimal(18,2)");
            e.Property(x => x.NewHoursCompleted).HasColumnType("decimal(18,2)");
            e.Property(x => x.PreviousStatus).HasMaxLength(50);
            e.Property(x => x.NewStatus).HasMaxLength(50);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasOne(x => x.TaskAssignment)
                .WithMany(ta => ta.ProgressUpdates)
                .HasForeignKey(x => x.TaskAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UpdatedByMember)
                .WithMany()
                .HasForeignKey(x => x.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

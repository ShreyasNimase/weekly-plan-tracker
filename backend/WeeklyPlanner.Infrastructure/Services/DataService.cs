using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Services;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Services;

public class DataService : IDataService
{
    private readonly AppDbContext _context;

    public DataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExportPayloadDto> ExportAsync(CancellationToken cancellationToken = default)
    {
        var teamMembers = await _context.TeamMembers.AsNoTracking().ToListAsync(cancellationToken);
        var backlogEntries = await _context.BacklogItems.AsNoTracking().ToListAsync(cancellationToken);
        var planningCycles = await _context.PlanningCycles.AsNoTracking().ToListAsync(cancellationToken);
        var categoryAllocations = await _context.CategoryAllocations.AsNoTracking().ToListAsync(cancellationToken);
        var cycleMembers = await _context.CycleMembers.AsNoTracking().ToListAsync(cancellationToken);
        var memberPlans = await _context.MemberPlans.AsNoTracking().ToListAsync(cancellationToken);
        var taskAssignments = await _context.TaskAssignments.AsNoTracking().ToListAsync(cancellationToken);
        var progressUpdates = await _context.ProgressUpdates.AsNoTracking().ToListAsync(cancellationToken);

        return new ExportPayloadDto
        {
            AppName = "WeeklyPlanTracker",
            DataVersion = 1,
            ExportedAt = DateTime.UtcNow,
            Data = new ExportDataDto
            {
                TeamMembers = teamMembers.Select(m => new ExportTeamMemberDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    IsLead = m.IsLead,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                BacklogEntries = backlogEntries.Select(b => new ExportBacklogItemDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    Category = b.Category,
                    Status = b.Status,
                    EstimatedEffort = b.EstimatedEffort,
                    CreatedBy = b.CreatedBy,
                    CreatedAt = b.CreatedAt
                }).ToList(),
                PlanningCycles = planningCycles.Select(c => new ExportPlanningCycleDto
                {
                    Id = c.Id,
                    PlanningDate = c.PlanningDate,
                    ExecutionStartDate = c.ExecutionStartDate,
                    ExecutionEndDate = c.ExecutionEndDate,
                    State = c.State,
                    TeamCapacity = c.TeamCapacity,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                CategoryAllocations = categoryAllocations.Select(ca => new ExportCategoryAllocationDto
                {
                    Id = ca.Id,
                    CycleId = ca.CycleId,
                    Category = ca.Category,
                    Percentage = ca.Percentage,
                    BudgetHours = ca.BudgetHours
                }).ToList(),
                CycleMembers = cycleMembers.Select(cm => new ExportCycleMemberDto
                {
                    Id = cm.Id,
                    CycleId = cm.CycleId,
                    MemberId = cm.MemberId
                }).ToList(),
                MemberPlans = memberPlans.Select(mp => new ExportMemberPlanDto
                {
                    Id = mp.Id,
                    CycleId = mp.CycleId,
                    MemberId = mp.MemberId,
                    IsReady = mp.IsReady,
                    TotalPlannedHours = mp.TotalPlannedHours
                }).ToList(),
                TaskAssignments = taskAssignments.Select(a => new ExportTaskAssignmentDto
                {
                    Id = a.Id,
                    MemberPlanId = a.MemberPlanId,
                    BacklogItemId = a.BacklogItemId,
                    CommittedHours = a.CommittedHours,
                    ProgressStatus = a.ProgressStatus,
                    HoursCompleted = a.HoursCompleted,
                    CreatedAt = a.CreatedAt
                }).ToList(),
                ProgressUpdates = progressUpdates.Select(pu => new ExportProgressUpdateDto
                {
                    Id = pu.Id,
                    TaskAssignmentId = pu.TaskAssignmentId,
                    Timestamp = pu.Timestamp,
                    PreviousHoursCompleted = pu.PreviousHoursCompleted,
                    NewHoursCompleted = pu.NewHoursCompleted,
                    PreviousStatus = pu.PreviousStatus,
                    NewStatus = pu.NewStatus,
                    Note = pu.Note,
                    UpdatedBy = pu.UpdatedBy
                }).ToList()
            }
        };
    }

    public async Task<(bool Success, string? Error)> ImportAsync(ExportPayloadDto payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(payload.AppName) || !string.Equals(payload.AppName, "WeeklyPlanTracker", StringComparison.OrdinalIgnoreCase))
            return (false, "Invalid backup: appName must be 'WeeklyPlanTracker'.");
        if (payload.DataVersion > 1)
            return (false, "Unsupported backup version. dataVersion must be <= 1.");
        if (payload.Data == null)
            return (false, "Backup file is missing the 'data' section.");
        var data = payload.Data;
        if (data.TeamMembers == null || data.BacklogEntries == null || data.PlanningCycles == null
            || data.CategoryAllocations == null || data.CycleMembers == null || data.MemberPlans == null
            || data.TaskAssignments == null || data.ProgressUpdates == null)
            return (false, "Backup data must contain all required arrays.");

        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _context.ProgressUpdates.ExecuteDeleteAsync(cancellationToken);
                    await _context.TaskAssignments.ExecuteDeleteAsync(cancellationToken);
                    await _context.MemberPlans.ExecuteDeleteAsync(cancellationToken);
                    await _context.CategoryAllocations.ExecuteDeleteAsync(cancellationToken);
                    await _context.CycleMembers.ExecuteDeleteAsync(cancellationToken);
                    await _context.PlanningCycles.ExecuteDeleteAsync(cancellationToken);
                    await _context.BacklogItems.ExecuteDeleteAsync(cancellationToken);
                    await _context.TeamMembers.ExecuteDeleteAsync(cancellationToken);

                    foreach (var d in data.TeamMembers)
                        _context.TeamMembers.Add(new TeamMember { Id = d.Id, Name = d.Name, IsLead = d.IsLead, IsActive = d.IsActive, CreatedAt = d.CreatedAt });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.BacklogEntries)
                        _context.BacklogItems.Add(new BacklogItem { Id = d.Id, Title = d.Title, Description = d.Description, Category = d.Category, Status = d.Status, EstimatedEffort = d.EstimatedEffort, CreatedBy = d.CreatedBy, CreatedAt = d.CreatedAt });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.PlanningCycles)
                        _context.PlanningCycles.Add(new PlanningCycle { Id = d.Id, PlanningDate = d.PlanningDate, ExecutionStartDate = d.ExecutionStartDate, ExecutionEndDate = d.ExecutionEndDate, State = d.State, TeamCapacity = d.TeamCapacity, CreatedAt = d.CreatedAt });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.CategoryAllocations)
                        _context.CategoryAllocations.Add(new CategoryAllocation { Id = d.Id, CycleId = d.CycleId, Category = d.Category, Percentage = d.Percentage, BudgetHours = d.BudgetHours });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.CycleMembers)
                        _context.CycleMembers.Add(new CycleMember { Id = d.Id, CycleId = d.CycleId, MemberId = d.MemberId });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.MemberPlans)
                        _context.MemberPlans.Add(new MemberPlan { Id = d.Id, CycleId = d.CycleId, MemberId = d.MemberId, IsReady = d.IsReady, TotalPlannedHours = d.TotalPlannedHours });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.TaskAssignments)
                        _context.TaskAssignments.Add(new TaskAssignment { Id = d.Id, MemberPlanId = d.MemberPlanId, BacklogItemId = d.BacklogItemId, CommittedHours = d.CommittedHours, ProgressStatus = d.ProgressStatus, HoursCompleted = d.HoursCompleted, CreatedAt = d.CreatedAt });
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var d in data.ProgressUpdates)
                        _context.ProgressUpdates.Add(new ProgressUpdate { Id = d.Id, TaskAssignmentId = d.TaskAssignmentId, Timestamp = d.Timestamp, PreviousHoursCompleted = d.PreviousHoursCompleted, NewHoursCompleted = d.NewHoursCompleted, PreviousStatus = d.PreviousStatus, NewStatus = d.NewStatus, Note = d.Note, UpdatedBy = d.UpdatedBy });
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        return (true, null);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Delete ALL existing data in FK-safe order
                await _context.ProgressUpdates.ExecuteDeleteAsync(cancellationToken);
                await _context.TaskAssignments.ExecuteDeleteAsync(cancellationToken);
                await _context.MemberPlans.ExecuteDeleteAsync(cancellationToken);
                await _context.CategoryAllocations.ExecuteDeleteAsync(cancellationToken);
                await _context.CycleMembers.ExecuteDeleteAsync(cancellationToken);
                await _context.PlanningCycles.ExecuteDeleteAsync(cancellationToken);
                await _context.BacklogItems.ExecuteDeleteAsync(cancellationToken);
                await _context.TeamMembers.ExecuteDeleteAsync(cancellationToken);

                // 2. Insert fresh team members — Alice is the ONLY lead
                var alice = new TeamMember { Id = Guid.NewGuid(), Name = "Alice Chen", IsLead = true, IsActive = true, CreatedAt = DateTime.UtcNow };
                var bob = new TeamMember { Id = Guid.NewGuid(), Name = "Bob Martinez", IsLead = false, IsActive = true, CreatedAt = DateTime.UtcNow };
                var carol = new TeamMember { Id = Guid.NewGuid(), Name = "Carol Singh", IsLead = false, IsActive = true, CreatedAt = DateTime.UtcNow };
                var dave = new TeamMember { Id = Guid.NewGuid(), Name = "Dave Kim", IsLead = false, IsActive = true, CreatedAt = DateTime.UtcNow };
                _context.TeamMembers.AddRange(alice, bob, carol, dave);
                await _context.SaveChangesAsync(cancellationToken);

                // 3. Insert fresh backlog items — all created by Alice
                var backlogItems = new List<BacklogItem>
                {
                    new() { Title = "Customer onboarding redesign", Description = "Revamp the onboarding flow for new customers.", Category = "CLIENT_FOCUSED", Status = "AVAILABLE", EstimatedEffort = 12m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Fix billing invoice formatting", Description = "Some invoices show wrong currency format.", Category = "CLIENT_FOCUSED", Status = "AVAILABLE", EstimatedEffort = 4m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Customer feedback dashboard", Description = "Build a dashboard showing NPS scores.", Category = "CLIENT_FOCUSED", Status = "AVAILABLE", EstimatedEffort = 16m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Migrate database to PostgreSQL 16", Description = "Upgrade from PG 14 to PG 16.", Category = "TECH_DEBT", Status = "AVAILABLE", EstimatedEffort = 20m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Remove deprecated API endpoints", Description = "Clean up v1 API routes.", Category = "TECH_DEBT", Status = "AVAILABLE", EstimatedEffort = 8m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Add unit tests for payment module", Description = "Coverage is below 50%.", Category = "TECH_DEBT", Status = "AVAILABLE", EstimatedEffort = 10m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Experiment with LLM-based search", Description = "Prototype semantic search using embeddings.", Category = "R_AND_D", Status = "AVAILABLE", EstimatedEffort = 15m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Evaluate new caching strategy", Description = "Compare Redis Cluster vs Memcached.", Category = "R_AND_D", Status = "AVAILABLE", EstimatedEffort = 6m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Build internal CLI tool", Description = "A command-line tool for common dev tasks.", Category = "R_AND_D", Status = "AVAILABLE", EstimatedEffort = 8m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Client SSO integration", Description = "Support SAML-based single sign-on for enterprise clients.", Category = "CLIENT_FOCUSED", Status = "AVAILABLE", EstimatedEffort = 18m, CreatedBy = alice.Id, CreatedAt = DateTime.UtcNow },
                };
                _context.BacklogItems.AddRange(backlogItems);
                await _context.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.ProgressUpdates.ExecuteDeleteAsync(cancellationToken);
                await _context.TaskAssignments.ExecuteDeleteAsync(cancellationToken);
                await _context.MemberPlans.ExecuteDeleteAsync(cancellationToken);
                await _context.CategoryAllocations.ExecuteDeleteAsync(cancellationToken);
                await _context.CycleMembers.ExecuteDeleteAsync(cancellationToken);
                await _context.PlanningCycles.ExecuteDeleteAsync(cancellationToken);
                await _context.BacklogItems.ExecuteDeleteAsync(cancellationToken);
                await _context.TeamMembers.ExecuteDeleteAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}

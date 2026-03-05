using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.API.Controllers;

[ApiController]
[Route("api")]
public class DataController : ControllerBase
{
    private readonly AppDbContext _db;

    public DataController(AppDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────
    // GET /api/export — Download full backup JSON
    // ─────────────────────────────────────────────
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var members     = await _db.TeamMembers.ToListAsync();
        var backlog     = await _db.BacklogItems.ToListAsync();
        var cycles      = await _db.PlanningCycles.ToListAsync();
        var cycleMembers= await _db.CycleMembers.ToListAsync();
        var budgets     = await _db.CategoryBudgets.ToListAsync();
        var assignments = await _db.TaskAssignments.ToListAsync();

        var backup = new
        {
            appName     = "WeeklyPlanTracker",
            dataVersion = 1,
            exportedAt  = DateTime.UtcNow.ToString("o"),
            data = new
            {
                teamMembers = members.Select(m => new
                {
                    id        = m.Id,
                    name      = m.Name,
                    isLead    = m.IsLead,
                    isActive  = m.IsActive,
                    createdAt = m.CreatedAt.ToString("o")
                }),
                backlogEntries = backlog.Select(b => new
                {
                    id             = b.Id,
                    title          = b.Title,
                    description    = b.Description,
                    category       = b.Category.ToString(),
                    status         = b.Status.ToString(),
                    priority       = b.Priority.ToString(),
                    estimatedHours = b.EstimatedHours,
                    createdAt      = b.CreatedAt.ToString("o")
                }),
                planningCycles = cycles.Select(c => new
                {
                    id            = c.Id,
                    weekStartDate = c.WeekStartDate.ToString("yyyy-MM-dd"),
                    status        = c.Status.ToString(),
                    createdAt     = c.CreatedAt.ToString("o")
                }),
                categoryAllocations = budgets.Select(cb => new
                {
                    id          = cb.Id,
                    cycleId     = cb.CycleId,
                    category    = cb.Category.ToString(),
                    percentage  = cb.Percentage,
                    hoursBudget = cb.HoursBudget
                }),
                memberPlans = cycleMembers.Select(cm => new
                {
                    id             = cm.Id,
                    cycleId        = cm.CycleId,
                    teamMemberId   = cm.TeamMemberId,
                    allocatedHours = cm.AllocatedHours,
                    isReady        = cm.IsReady
                }),
                taskAssignments = assignments.Select(a => new
                {
                    id            = a.Id,
                    cycleMemberId = a.CycleMemberId,
                    backlogItemId = a.BacklogItemId,
                    plannedHours  = a.PlannedHours,
                    createdAt     = a.CreatedAt.ToString("o")
                }),
                progressUpdates = Array.Empty<object>() // placeholder — extend when progress tracking is added
            }
        };

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var fileName = $"weekly-planner-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    // ─────────────────────────────────────────────
    // POST /api/import — Restore from backup JSON
    // ─────────────────────────────────────────────
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] JsonElement payload)
    {
        // Validate structure
        if (!payload.TryGetProperty("appName", out var appNameEl) ||
            appNameEl.GetString() != "WeeklyPlanTracker")
            return BadRequest(new { message = "Invalid backup file: appName must be 'WeeklyPlanTracker'." });

        if (!payload.TryGetProperty("dataVersion", out var versionEl) ||
            versionEl.GetInt32() != 1)
            return BadRequest(new { message = "Unsupported backup version. Expected dataVersion 1." });

        if (!payload.TryGetProperty("data", out var data))
            return BadRequest(new { message = "Backup file is missing the 'data' section." });

        // Wipe in FK-safe order (children first)
        await WipeAllAsync();

        // ── Re-seed from backup ────────────────────────────────────────
        var idMap = new Dictionary<string, Guid>(); // old guid string → new Guid

        // 1. Team Members
        if (data.TryGetProperty("teamMembers", out var membersEl))
        {
            foreach (var m in membersEl.EnumerateArray())
            {
                var newId = Guid.NewGuid();
                if (m.TryGetProperty("id", out var oldId))
                    idMap[oldId.GetString()!] = newId;

                _db.TeamMembers.Add(new TeamMember
                {
                    Id        = newId,
                    Name      = m.GetProperty("name").GetString()!,
                    IsLead    = m.TryGetProperty("isLead",   out var il) && il.GetBoolean(),
                    IsActive  = !m.TryGetProperty("isActive", out var ia) || ia.GetBoolean(),
                    CreatedAt = m.TryGetProperty("createdAt", out var ca) && DateTime.TryParse(ca.GetString(), out var dt) ? dt : DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        // 2. Backlog Items
        if (data.TryGetProperty("backlogEntries", out var backlogEl))
        {
            foreach (var b in backlogEl.EnumerateArray())
            {
                var newId = Guid.NewGuid();
                if (b.TryGetProperty("id", out var oldId))
                    idMap[oldId.GetString()!] = newId;

                _db.BacklogItems.Add(new BacklogItem
                {
                    Id             = newId,
                    Title          = b.GetProperty("title").GetString()!,
                    Description    = b.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    Category       = Enum.TryParse<BacklogCategory>(b.TryGetProperty("category", out var cat) ? cat.GetString() : "Feature", out var c) ? c : BacklogCategory.Feature,
                    Status         = Enum.TryParse<BacklogStatus>  (b.TryGetProperty("status",   out var st)  ? st.GetString()  : "Active",   out var s) ? s : BacklogStatus.Active,
                    Priority       = Enum.TryParse<BacklogPriority>(b.TryGetProperty("priority", out var pr)  ? pr.GetString()  : "Medium",  out var p) ? p : BacklogPriority.Medium,
                    EstimatedHours = b.TryGetProperty("estimatedHours", out var eh) && eh.ValueKind != JsonValueKind.Null ? eh.GetDecimal() : null,
                    CreatedAt      = b.TryGetProperty("createdAt", out var ca2) && DateTime.TryParse(ca2.GetString(), out var dt2) ? dt2 : DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        // 3. Planning Cycles
        if (data.TryGetProperty("planningCycles", out var cyclesEl))
        {
            foreach (var c in cyclesEl.EnumerateArray())
            {
                var newId = Guid.NewGuid();
                if (c.TryGetProperty("id", out var oldId))
                    idMap[oldId.GetString()!] = newId;

                _db.PlanningCycles.Add(new PlanningCycle
                {
                    Id            = newId,
                    WeekStartDate = DateTime.Parse(c.GetProperty("weekStartDate").GetString()!),
                    Status        = Enum.TryParse<CycleStatus>(c.TryGetProperty("status", out var st) ? st.GetString() : "Setup", out var cs) ? cs : CycleStatus.Setup,
                    CreatedAt     = c.TryGetProperty("createdAt", out var ca) && DateTime.TryParse(ca.GetString(), out var dt) ? dt : DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        // 4. Category Allocations
        if (data.TryGetProperty("categoryAllocations", out var budgetsEl))
        {
            foreach (var cb in budgetsEl.EnumerateArray())
            {
                var oldCycleId = cb.TryGetProperty("cycleId", out var ci) ? ci.GetString() : null;
                if (oldCycleId == null || !idMap.TryGetValue(oldCycleId, out var newCycleId)) continue;

                _db.CategoryBudgets.Add(new CategoryBudget
                {
                    Id          = Guid.NewGuid(),
                    CycleId     = newCycleId,
                    Category    = Enum.TryParse<BacklogCategory>(cb.TryGetProperty("category", out var cat) ? cat.GetString() : "Feature", out var bcat) ? bcat : BacklogCategory.Feature,
                    Percentage  = cb.TryGetProperty("percentage",  out var pct) ? pct.GetDecimal() : 0,
                    HoursBudget = cb.TryGetProperty("hoursBudget", out var hb)  ? hb.GetDecimal()  : 0
                });
            }
            await _db.SaveChangesAsync();
        }

        // 5. Member Plans (CycleMembers)
        if (data.TryGetProperty("memberPlans", out var plansEl))
        {
            foreach (var cm in plansEl.EnumerateArray())
            {
                var oldCycleId  = cm.TryGetProperty("cycleId",      out var ci)  ? ci.GetString()  : null;
                var oldMemberId = cm.TryGetProperty("teamMemberId", out var mi)  ? mi.GetString()  : null;
                if (oldCycleId == null || oldMemberId == null) continue;
                if (!idMap.TryGetValue(oldCycleId,  out var newCycleId))  continue;
                if (!idMap.TryGetValue(oldMemberId, out var newMemberId)) continue;

                var oldCmId = cm.TryGetProperty("id", out var cmId) ? cmId.GetString() : null;
                var newCmId = Guid.NewGuid();
                if (oldCmId != null) idMap[oldCmId] = newCmId;

                _db.CycleMembers.Add(new CycleMember
                {
                    Id             = newCmId,
                    CycleId        = newCycleId,
                    TeamMemberId   = newMemberId,
                    AllocatedHours = cm.TryGetProperty("allocatedHours", out var ah) ? ah.GetDecimal() : 30,
                    IsReady        = cm.TryGetProperty("isReady", out var ir) && ir.GetBoolean()
                });
            }
            await _db.SaveChangesAsync();
        }

        // 6. Task Assignments
        if (data.TryGetProperty("taskAssignments", out var assignEl))
        {
            foreach (var a in assignEl.EnumerateArray())
            {
                var oldCmId      = a.TryGetProperty("cycleMemberId", out var cm) ? cm.GetString() : null;
                var oldBacklogId = a.TryGetProperty("backlogItemId", out var bi) ? bi.GetString() : null;
                if (oldCmId == null || oldBacklogId == null) continue;
                if (!idMap.TryGetValue(oldCmId,      out var newCmId))      continue;
                if (!idMap.TryGetValue(oldBacklogId, out var newBacklogId)) continue;

                _db.TaskAssignments.Add(new TaskAssignment
                {
                    Id            = Guid.NewGuid(),
                    CycleMemberId = newCmId,
                    BacklogItemId = newBacklogId,
                    PlannedHours  = a.TryGetProperty("plannedHours", out var ph) ? ph.GetDecimal() : 0,
                    CreatedAt     = a.TryGetProperty("createdAt", out var ca) && DateTime.TryParse(ca.GetString(), out var dt) ? dt : DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Import successful. All data has been replaced with the backup." });
    }

    // ─────────────────────────────────────────────
    // POST /api/seed — Populate with sample data
    // ─────────────────────────────────────────────
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        // Don't seed if data already exists
        if (await _db.TeamMembers.AnyAsync())
            return BadRequest(new { message = "Database already contains data. Reset first before seeding." });

        // ── Team Members ──────────────────────────────────────────────────
        var alice = new TeamMember { Name = "Alice Chen",    IsLead = true };
        var bob   = new TeamMember { Name = "Bob Martinez",  IsLead = false };
        var carol = new TeamMember { Name = "Carol Singh",   IsLead = false };
        var dave  = new TeamMember { Name = "Dave Kim",      IsLead = false };
        _db.TeamMembers.AddRange(alice, bob, carol, dave);
        await _db.SaveChangesAsync();

        // ── Backlog Items ─────────────────────────────────────────────────
        var backlogItems = new List<BacklogItem>
        {
            new() { Title = "Customer onboarding redesign",         Category = BacklogCategory.Feature,  Priority = BacklogPriority.High,   EstimatedHours = 8m  },
            new() { Title = "Fix billing invoice formatting",        Category = BacklogCategory.Bug,      Priority = BacklogPriority.High,   EstimatedHours = 3m  },
            new() { Title = "Customer feedback dashboard",           Category = BacklogCategory.Feature,  Priority = BacklogPriority.Medium, EstimatedHours = 6m  },
            new() { Title = "Migrate database to PostgreSQL 16",     Category = BacklogCategory.TechDebt, Priority = BacklogPriority.Medium, EstimatedHours = 12m },
            new() { Title = "Remove deprecated API endpoints",       Category = BacklogCategory.TechDebt, Priority = BacklogPriority.Low,    EstimatedHours = 4m  },
            new() { Title = "Add unit tests for payment module",     Category = BacklogCategory.TechDebt, Priority = BacklogPriority.High,   EstimatedHours = 5m  },
            new() { Title = "Experiment with LLM-based search",      Category = BacklogCategory.Learning, Priority = BacklogPriority.Low,    EstimatedHours = 8m  },
            new() { Title = "Evaluate new caching strategy",         Category = BacklogCategory.Learning, Priority = BacklogPriority.Medium, EstimatedHours = 4m  },
            new() { Title = "Build internal CLI tool",               Category = BacklogCategory.Feature,  Priority = BacklogPriority.Low,    EstimatedHours = 6m  },
            new() { Title = "Client SSO integration",                Category = BacklogCategory.Feature,  Priority = BacklogPriority.High,   EstimatedHours = 10m }
        };
        _db.BacklogItems.AddRange(backlogItems);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Sample data seeded successfully.",
            teamMembers  = 4,
            backlogItems = backlogItems.Count
        });
    }

    // ─────────────────────────────────────────────
    // DELETE /api/reset — Wipe everything
    // ─────────────────────────────────────────────
    [HttpDelete("reset")]
    public async Task<IActionResult> Reset()
    {
        await WipeAllAsync();
        return Ok(new { message = "Application reset. All data has been deleted." });
    }

    // ─────────────────────────────────────────────
    // Shared — delete in FK-safe dependency order
    // ─────────────────────────────────────────────
    private async Task WipeAllAsync()
    {
        // Delete deepest dependencies first
        _db.TaskAssignments.RemoveRange(_db.TaskAssignments);
        await _db.SaveChangesAsync();

        _db.CycleMembers.RemoveRange(_db.CycleMembers);
        _db.CategoryBudgets.RemoveRange(_db.CategoryBudgets);
        await _db.SaveChangesAsync();

        _db.PlanningCycles.RemoveRange(_db.PlanningCycles);
        await _db.SaveChangesAsync();

        _db.BacklogItems.RemoveRange(_db.BacklogItems);
        await _db.SaveChangesAsync();

        _db.TeamMembers.RemoveRange(_db.TeamMembers);
        await _db.SaveChangesAsync();
    }
}

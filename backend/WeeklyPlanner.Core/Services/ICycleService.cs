using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for planning cycle operations.</summary>
public interface ICycleService
{
    /// <summary>Starts a new cycle (next Tuesday). Fails if an active cycle exists.</summary>
    Task<(CycleDto? Result, string? Error)> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Setup cycle: planningDate (Tuesday), members, category allocations. Replaces existing setup.</summary>
    Task<(CycleDto? Result, string? Error)> SetupAsync(Guid cycleId, SetupCycleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Opens planning (SETUP → PLANNING).</summary>
    Task<(CycleDto? Result, string? Error)> OpenAsync(Guid cycleId, CancellationToken cancellationToken = default);

    /// <summary>Gets the active cycle (state SETUP, PLANNING, or FROZEN), or null.</summary>
    Task<CycleDto?> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Freezes the plan (PLANNING → FROZEN). Validates each member 30h and each category budget.</summary>
    Task<(CycleDto? Result, List<string>? Errors)> FreezeAsync(Guid cycleId, CancellationToken cancellationToken = default);

    /// <summary>Completes the cycle (FROZEN → COMPLETED) and updates BacklogItem statuses.</summary>
    Task<(CycleDto? Result, string? Error)> CompleteAsync(Guid cycleId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the cycle (only SETUP or PLANNING). Resets affected BacklogItems to AVAILABLE if needed.</summary>
    Task<string?> DeleteAsync(Guid cycleId, CancellationToken cancellationToken = default);

    /// <summary>Gets history: cycles with state FROZEN or COMPLETED, sorted by PlanningDate descending.</summary>
    Task<IReadOnlyList<CycleDto>> GetHistoryAsync(CancellationToken cancellationToken = default);
}

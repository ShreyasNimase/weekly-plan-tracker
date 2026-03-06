using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for data export, import, seed, and reset.</summary>
public interface IDataService
{
    /// <summary>Exports all data for backup download.</summary>
    Task<ExportPayloadDto> ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>Imports from backup payload. Returns error message on validation failure.</summary>
    Task<(bool Success, string? Error)> ImportAsync(ExportPayloadDto payload, CancellationToken cancellationToken = default);

    /// <summary>Seeds team members and backlog items; clears cycle-related data. Does not erase existing team/backlog.</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes all data in FK-safe order.</summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

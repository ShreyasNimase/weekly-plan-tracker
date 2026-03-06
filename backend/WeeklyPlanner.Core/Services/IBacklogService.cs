using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for backlog operations.</summary>
public interface IBacklogService
{
    /// <summary>Creates a new backlog item. Status = AVAILABLE.</summary>
    Task<(BacklogItemDto? Result, string? Error)> CreateAsync(CreateBacklogItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets items with optional category, status (default AVAILABLE+IN_PLAN, ARCHIVED, COMPLETED, ALL), and search. Sorted by CreatedAt descending.</summary>
    Task<IReadOnlyList<BacklogItemDto>> GetAllAsync(string? category, string? status, string? search, CancellationToken cancellationToken = default);

    /// <summary>Gets a single item by id.</summary>
    Task<BacklogItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates title, description, estimatedEffort only. Category is not updatable.</summary>
    Task<(BacklogItemDto? Result, string? Error)> UpdateAsync(Guid id, UpdateBacklogItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Archives the item. Fails if status is IN_PLAN.</summary>
    Task<(BacklogItemDto? Result, string? Error)> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the item. Only allowed when status is AVAILABLE or ARCHIVED. Returns 204.</summary>
    Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

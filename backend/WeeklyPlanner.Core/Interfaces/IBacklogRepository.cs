using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Repository for backlog items. Category/Status are strings: CLIENT_FOCUSED, TECH_DEBT, R_AND_D; AVAILABLE, IN_PLAN, COMPLETED, ARCHIVED.
/// </summary>
public interface IBacklogRepository
{
    /// <summary>Adds a new backlog item and persists.</summary>
    Task<BacklogItem> AddAsync(BacklogItem item, CancellationToken cancellationToken = default);

    /// <summary>Gets all items, optionally filtered by category, status, and search text.</summary>
    Task<IEnumerable<BacklogItem>> GetAllAsync(string? category, string? status, string? search, CancellationToken cancellationToken = default);

    /// <summary>Gets a single item by id.</summary>
    Task<BacklogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing backlog item.</summary>
    Task<BacklogItem> UpdateAsync(BacklogItem item, CancellationToken cancellationToken = default);

    /// <summary>Deletes a backlog item by id.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

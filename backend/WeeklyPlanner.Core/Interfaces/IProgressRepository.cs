using WeeklyPlanner.Core.Entities;

namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Repository for progress update audit records.
/// </summary>
public interface IProgressRepository
{
    /// <summary>Adds a new progress update record.</summary>
    Task<ProgressUpdate> AddAsync(ProgressUpdate progressUpdate, CancellationToken cancellationToken = default);

    /// <summary>Gets a progress update by id.</summary>
    Task<ProgressUpdate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all progress updates for a task assignment, ordered by Timestamp descending.</summary>
    Task<IEnumerable<ProgressUpdate>> GetByTaskAssignmentIdAsync(Guid taskAssignmentId, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing progress update (rare).</summary>
    Task<ProgressUpdate> UpdateAsync(ProgressUpdate progressUpdate, CancellationToken cancellationToken = default);

    /// <summary>Deletes a progress update by id.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

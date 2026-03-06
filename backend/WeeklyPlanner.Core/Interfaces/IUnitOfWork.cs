namespace WeeklyPlanner.Core.Interfaces;

/// <summary>
/// Unit of Work for coordinating repository saves and handling Azure SQL transient errors.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes to the database.
    /// Callers should catch <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> and
    /// transient Azure SQL errors for retry logic.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

/// <summary>
/// Repository for progress update audit records. Call <see cref="IUnitOfWork.SaveChangesAsync"/> after add/update/delete.
/// </summary>
public class ProgressRepository : IProgressRepository
{
    private readonly AppDbContext _context;

    public ProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ProgressUpdate> AddAsync(ProgressUpdate progressUpdate, CancellationToken cancellationToken = default)
    {
        _context.ProgressUpdates.Add(progressUpdate);
        await _context.SaveChangesAsync(cancellationToken);
        return progressUpdate;
    }

    /// <inheritdoc />
    public async Task<ProgressUpdate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProgressUpdates
            .Include(pu => pu.TaskAssignment)
            .Include(pu => pu.UpdatedByMember)
            .FirstOrDefaultAsync(pu => pu.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProgressUpdate>> GetByTaskAssignmentIdAsync(Guid taskAssignmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ProgressUpdates
            .Where(pu => pu.TaskAssignmentId == taskAssignmentId)
            .OrderByDescending(pu => pu.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProgressUpdate> UpdateAsync(ProgressUpdate progressUpdate, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(progressUpdate).State == EntityState.Detached)
            _context.ProgressUpdates.Update(progressUpdate);
        await _context.SaveChangesAsync(cancellationToken);
        return progressUpdate;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var update = await _context.ProgressUpdates.FindAsync([id], cancellationToken);
        if (update is not null)
        {
            _context.ProgressUpdates.Remove(update);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

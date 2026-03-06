using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

/// <summary>
/// Repository for backlog items. Category/Status are strings. Call <see cref="IUnitOfWork.SaveChangesAsync"/> after add/update/delete.
/// </summary>
public class BacklogRepository : IBacklogRepository
{
    private readonly AppDbContext _context;

    public BacklogRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<BacklogItem> AddAsync(BacklogItem item, CancellationToken cancellationToken = default)
    {
        _context.BacklogItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BacklogItem>> GetAllAsync(string? category, string? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.BacklogItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(i => i.Category == category);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                _ = query; // no status filter
            else if (status.Equals("ARCHIVED", StringComparison.OrdinalIgnoreCase))
                query = query.Where(i => i.Status == "ARCHIVED");
            else if (status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                query = query.Where(i => i.Status == "COMPLETED");
            else
                query = query.Where(i => i.Status == status);
        }
        else
            query = query.Where(i => i.Status == "AVAILABLE" || i.Status == "IN_PLAN");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.Trim().ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(lower) ||
                (i.Description != null && i.Description.ToLower().Contains(lower)));
        }

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BacklogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BacklogItems.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BacklogItem> UpdateAsync(BacklogItem item, CancellationToken cancellationToken = default)
    {
        _context.BacklogItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.BacklogItems.FindAsync([id], cancellationToken);
        if (item is not null)
        {
            _context.BacklogItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

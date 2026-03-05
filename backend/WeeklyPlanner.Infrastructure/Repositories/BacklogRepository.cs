using Microsoft.EntityFrameworkCore;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Infrastructure.Data;

namespace WeeklyPlanner.Infrastructure.Repositories;

public class BacklogRepository : IBacklogRepository
{
    private readonly AppDbContext _context;

    public BacklogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BacklogItem> AddAsync(BacklogItem item)
    {
        _context.BacklogItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<IEnumerable<BacklogItem>> GetAllAsync(
        BacklogCategory? category,
        BacklogStatus? status,
        string? search)
    {
        var query = _context.BacklogItems.AsQueryable();

        // Filter by category if provided
        if (category.HasValue)
            query = query.Where(i => i.Category == category.Value);

        // Filter by status if provided; default to Active only when null
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        else
            query = query.Where(i => i.Status == BacklogStatus.Active);

        // Case-insensitive search on title and description
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.Trim().ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(lower) ||
                (i.Description != null && i.Description.ToLower().Contains(lower)));
        }

        return await query
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<BacklogItem?> GetByIdAsync(Guid id)
    {
        return await _context.BacklogItems.FindAsync(id);
    }

    public async Task<BacklogItem> UpdateAsync(BacklogItem item)
    {
        _context.BacklogItems.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.BacklogItems.FindAsync(id);
        if (item is not null)
        {
            _context.BacklogItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}

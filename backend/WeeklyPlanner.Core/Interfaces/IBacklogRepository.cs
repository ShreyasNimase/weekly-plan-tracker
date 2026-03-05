using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Enums;

namespace WeeklyPlanner.Core.Interfaces;

public interface IBacklogRepository
{
    Task<BacklogItem> AddAsync(BacklogItem item);
    Task<IEnumerable<BacklogItem>> GetAllAsync(BacklogCategory? category, BacklogStatus? status, string? search);
    Task<BacklogItem?> GetByIdAsync(Guid id);
    Task<BacklogItem> UpdateAsync(BacklogItem item);
    Task DeleteAsync(Guid id);
}

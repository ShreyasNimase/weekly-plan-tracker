using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Entities;
using WeeklyPlanner.Core.Interfaces;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.Infrastructure.Services;

public class BacklogService : IBacklogService
{
    private readonly IBacklogRepository _repo;

    public BacklogService(IBacklogRepository repo)
    {
        _repo = repo;
    }

    public async Task<(BacklogItemDto? Result, string? Error)> CreateAsync(CreateBacklogItemRequest request, CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim() ?? "";
        if (string.IsNullOrEmpty(title))
            return (null, "Title is required.");
        if (title.Length > 200)
            return (null, "Title cannot exceed 200 characters.");
        if (request.Description != null && request.Description.Length > 5000)
            return (null, "Description cannot exceed 5000 characters.");

        var category = request.Category?.Trim().ToUpperInvariant();
        if (category != "CLIENT_FOCUSED" && category != "TECH_DEBT" && category != "R_AND_D")
            return (null, "Category must be one of: CLIENT_FOCUSED, TECH_DEBT, R_AND_D.");

        if (request.EstimatedEffort.HasValue)
        {
            var e = request.EstimatedEffort.Value;
            if (e <= 0 || e > 999.5m)
                return (null, "Estimated effort must be greater than 0 and at most 999.5.");
            if ((e * 2) % 1 != 0)
                return (null, "Estimated effort must be in 0.5 increments.");
        }

        var createdBy = request.CreatedBy ?? Guid.Empty;

        var item = new BacklogItem
        {
            Title = title,
            Description = request.Description?.Trim(),
            Category = category!,
            Status = "AVAILABLE",
            EstimatedEffort = request.EstimatedEffort,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _repo.AddAsync(item, cancellationToken);
        return (ToDto(created), null);
    }

    public async Task<IReadOnlyList<BacklogItemDto>> GetAllAsync(string? category, string? status, string? search, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetAllAsync(category, status, search, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<BacklogItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repo.GetByIdAsync(id, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<(BacklogItemDto? Result, string? Error)> UpdateAsync(Guid id, UpdateBacklogItemRequest request, CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim() ?? "";
        if (string.IsNullOrEmpty(title))
            return (null, "Title is required.");
        if (title.Length > 200)
            return (null, "Title cannot exceed 200 characters.");
        if (request.Description != null && request.Description.Length > 5000)
            return (null, "Description cannot exceed 5000 characters.");

        if (request.EstimatedEffort.HasValue)
        {
            var e = request.EstimatedEffort.Value;
            if (e <= 0 || e > 999.5m)
                return (null, "Estimated effort must be greater than 0 and at most 999.5.");
            if ((e * 2) % 1 != 0)
                return (null, "Estimated effort must be in 0.5 increments.");
        }

        var item = await _repo.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return (null, "Backlog item not found.");

        item.Title = title;
        item.Description = request.Description?.Trim();
        item.EstimatedEffort = request.EstimatedEffort;
        await _repo.UpdateAsync(item, cancellationToken);
        return (ToDto(item), null);
    }

    public async Task<(BacklogItemDto? Result, string? Error)> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repo.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return (null, "Backlog item not found.");
        if (item.Status == "IN_PLAN")
            return (null, "This item is part of an active plan.");
        if (item.Status == "ARCHIVED")
            return (ToDto(item), null);

        item.Status = "ARCHIVED";
        await _repo.UpdateAsync(item, cancellationToken);
        return (ToDto(item), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repo.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return (false, "Backlog item not found.");
        if (item.Status != "AVAILABLE" && item.Status != "ARCHIVED")
            return (false, "Only items with status AVAILABLE or ARCHIVED can be deleted.");

        await _repo.DeleteAsync(id, cancellationToken);
        return (true, null);
    }

    private static BacklogItemDto ToDto(BacklogItem i) => new()
    {
        Id = i.Id,
        Title = i.Title,
        Description = i.Description,
        Category = i.Category,
        Status = i.Status,
        EstimatedEffort = i.EstimatedEffort,
        CreatedBy = i.CreatedBy,
        CreatedAt = i.CreatedAt
    };
}

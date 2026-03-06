using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.Core.Services;

/// <summary>Application service for dashboard data.</summary>
public interface IDashboardService
{
    /// <summary>Gets dashboard for active cycle (FROZEN preferred, else PLANNING).</summary>
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

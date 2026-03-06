using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>Dashboard API: active cycle summary, category and member breakdown.</summary>
[ApiController]
[Route("api")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Gets dashboard for active cycle (FROZEN preferred, else PLANNING).</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }
}

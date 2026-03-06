using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;

namespace WeeklyPlanner.API.Controllers;

/// <summary>Data utility APIs: export, import, seed, reset.</summary>
[ApiController]
[Route("api")]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;

    public DataController(IDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>Exports all data as JSON file download.</summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(CancellationToken cancellationToken = default)
    {
        var payload = await _dataService.ExportAsync(cancellationToken);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var fileName = $"weeklyplantracker-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    /// <summary>Imports from exported JSON. Replaces all data in a transaction.</summary>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import([FromBody] ExportPayloadDto payload, CancellationToken cancellationToken = default)
    {
        var (success, error) = await _dataService.ImportAsync(payload, cancellationToken);
        if (!success)
            return BadRequest(new { message = error });
        return Ok(new { message = "Import successful." });
    }

    /// <summary>Seeds team members and backlog items; clears cycle-related data. Does not erase existing team/backlog.</summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken = default)
    {
        await _dataService.SeedAsync(cancellationToken);
        return Ok(new { message = "Seed completed." });
    }

    /// <summary>Deletes all data in FK-safe order.</summary>
    [HttpDelete("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken = default)
    {
        await _dataService.ResetAsync(cancellationToken);
        return NoContent();
    }
}

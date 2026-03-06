using Microsoft.EntityFrameworkCore;
using Moq;
using WeeklyPlanner.Core.DTOs;
using WeeklyPlanner.Core.Services;
using WeeklyPlanner.Infrastructure.Data;
using WeeklyPlanner.Infrastructure.Services;
using Xunit;

namespace WeeklyPlanner.Tests;

public class DataServiceTests
{
    [Fact]
    public void ExportPayload_Structure_HasAppNameAndDataVersion()
    {
        var payload = new ExportPayloadDto
        {
            AppName = "WeeklyPlanTracker",
            DataVersion = 1,
            ExportedAt = DateTime.UtcNow,
            Data = new ExportDataDto
            {
                TeamMembers = [],
                BacklogEntries = [],
                PlanningCycles = [],
                CategoryAllocations = [],
                CycleMembers = [],
                MemberPlans = [],
                TaskAssignments = [],
                ProgressUpdates = []
            }
        };
        Assert.Equal("WeeklyPlanTracker", payload.AppName);
        Assert.Equal(1, payload.DataVersion);
        Assert.NotNull(payload.Data);
        Assert.NotNull(payload.Data.TeamMembers);
        Assert.NotNull(payload.Data.ProgressUpdates);
    }

    [Fact]
    public async Task Import_InvalidAppName_ReturnsError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "ImportInvalidApp_" + Guid.NewGuid())
            .Options;
        await using var context = new AppDbContext(options);
        var service = new DataService(context);

        var payload = new ExportPayloadDto { AppName = "OtherApp", DataVersion = 1, Data = new ExportDataDto() };
        var (success, error) = await service.ImportAsync(payload);
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("WeeklyPlanTracker", error);
    }

    [Fact]
    public async Task Import_DataVersionGreaterThan1_ReturnsError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "ImportVersion_" + Guid.NewGuid())
            .Options;
        await using var context = new AppDbContext(options);
        var service = new DataService(context);

        var payload = new ExportPayloadDto { AppName = "WeeklyPlanTracker", DataVersion = 2, Data = new ExportDataDto() };
        var (success, error) = await service.ImportAsync(payload);
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("version", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_NullData_ReturnsError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "ImportNullData_" + Guid.NewGuid())
            .Options;
        await using var context = new AppDbContext(options);
        var service = new DataService(context);

        var payload = new ExportPayloadDto { AppName = "WeeklyPlanTracker", DataVersion = 1, Data = null! };
        var (success, error) = await service.ImportAsync(payload);
        Assert.False(success);
        Assert.NotNull(error);
    }
}

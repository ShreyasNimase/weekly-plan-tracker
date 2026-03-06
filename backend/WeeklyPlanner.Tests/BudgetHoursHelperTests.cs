using WeeklyPlanner.Core.Helpers;
using Xunit;

namespace WeeklyPlanner.Tests;

/// <summary>Tests for budget hours calculation: rounding to 0.5 and difference applied to highest-pct category.</summary>
public class BudgetHoursHelperTests
{
    [Fact]
    public void CalculateBudgetHours_ThreeEqualCategories_SumEqualsCap()
    {
        // 2 members = 60h cap; 33+33+34 = 100; raw: 19.8, 19.8, 20.4 → 20, 20, 20.5 = 60.5; diff -0.5 to first (33)
        var allocations = new (string Category, int Percentage)[]
        {
            ("CLIENT_FOCUSED", 34),
            ("TECH_DEBT", 33),
            ("INTERNAL", 33)
        };
        var result = BudgetHoursHelper.CalculateBudgetHours(2, allocations);
        Assert.Equal(3, result.Count);
        decimal sum = result.Values.Sum();
        Assert.Equal(60m, sum);
    }

    [Fact]
    public void CalculateBudgetHours_OneMember_ThirtyHoursTotal()
    {
        var allocations = new (string Category, int Percentage)[]
        {
            ("A", 50),
            ("B", 30),
            ("C", 20)
        };
        var result = BudgetHoursHelper.CalculateBudgetHours(1, allocations);
        Assert.Equal(30m, result.Values.Sum());
        Assert.Equal(15m, result["A"]);   // 50% of 30
        Assert.Equal(9m, result["B"]);     // 30%
        Assert.Equal(6m, result["C"]);     // 20%
    }

    [Fact]
    public void CalculateBudgetHours_RoundingEdgeCase_DifferenceToHighestPct()
    {
        // 1 member = 30h. 33+33+34. Raw: 9.9→10, 9.9→10, 10.2→10. Sum=30 ok. If we had 34+33+33: 10.2→10, 9.9→10, 9.9→10 = 30.
        // Use 3 members = 90h. 34+33+33: 30.6→30.5, 29.7→30, 29.7→30. Sum=90.5. Diff -0.5 → add to first: 30.5-0.5=30? No, diff = cap - sum = 90 - 90.5 = -0.5, so we add -0.5 to first → 30.
        var allocations = new (string Category, int Percentage)[]
        {
            ("HIGH", 34),
            ("MID", 33),
            ("LOW", 33)
        };
        var result = BudgetHoursHelper.CalculateBudgetHours(3, allocations);
        Assert.Equal(90m, result.Values.Sum());
        Assert.True(result["HIGH"] >= 29m && result["HIGH"] <= 31m);
    }

    [Fact]
    public void CalculateBudgetHours_ZeroMemberCount_ReturnsEmpty()
    {
        var allocations = new (string Category, int Percentage)[] { ("A", 100) };
        var result = BudgetHoursHelper.CalculateBudgetHours(0, allocations);
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateBudgetHours_NullOrEmptyAllocations_ReturnsEmpty()
    {
        var empty = BudgetHoursHelper.CalculateBudgetHours(1, Array.Empty<(string, int)>());
        Assert.Empty(empty);
    }
}

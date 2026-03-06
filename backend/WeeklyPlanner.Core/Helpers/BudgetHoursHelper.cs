namespace WeeklyPlanner.Core.Helpers;

/// <summary>
/// Calculates category budget hours so they sum exactly to capacity (nearest 0.5).
/// raw[cat] = Round(cap × pct/100 × 2) / 2; if sum(raw) != cap, add difference to highest-pct category.
/// </summary>
public static class BudgetHoursHelper
{
    /// <summary>
    /// Returns budget hours per category. allocations must have Category and Percentage set.
    /// </summary>
    /// <param name="memberCount">Number of participating members.</param>
    /// <param name="allocations">Category and Percentage (0-100) for each; typically 3 entries.</param>
    /// <returns>Dictionary of category -> budget hours (rounded to 0.5, sum = memberCount * 30).</returns>
    public static Dictionary<string, decimal> CalculateBudgetHours(int memberCount, IReadOnlyList<(string Category, int Percentage)> allocations)
    {
        if (memberCount <= 0 || allocations == null || allocations.Count == 0)
            return new Dictionary<string, decimal>();

        decimal cap = memberCount * 30m;
        var result = new Dictionary<string, decimal>(allocations.Count);
        var ordered = allocations.OrderByDescending(a => a.Percentage).ToList();

        foreach (var (category, pct) in ordered)
        {
            decimal raw = Math.Round(cap * pct / 100m * 2, MidpointRounding.AwayFromZero) / 2m;
            result[category] = raw;
        }

        decimal sum = result.Values.Sum();
        if (sum != cap && ordered.Count > 0)
        {
            decimal diff = cap - sum;
            string firstCategory = ordered[0].Category;
            result[firstCategory] = result[firstCategory] + diff;
        }

        return result;
    }
}

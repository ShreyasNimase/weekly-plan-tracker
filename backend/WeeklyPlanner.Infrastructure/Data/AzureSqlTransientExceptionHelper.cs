using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WeeklyPlanner.Infrastructure.Data;

/// <summary>
/// Helper for detecting Azure SQL transient errors so callers can retry.
/// EF Core's EnableRetryOnFailure already retries common transient errors; use this
/// when you need custom handling or to log/classify failures.
/// </summary>
public static class AzureSqlTransientExceptionHelper
{
    /// <summary>
    /// Returns true if the exception is a known Azure SQL transient error
    /// (e.g. connection timeout, throttling, temporary server unavailable).
    /// </summary>
    /// <param name="ex">The exception thrown from a database operation.</param>
    /// <returns>True if the operation can be retried.</returns>
    public static bool IsTransient(Exception ex)
    {
        if (ex is SqlException sqlEx)
        {
            foreach (SqlError err in sqlEx.Errors)
            {
                switch (err.Number)
                {
                    case -2:   // Timeout
                    case -1:   // Connection broken
                    case 20:   // Instance not found
                    case 64:   // Network error
                    case 233:  // Connection initializing
                    case 10053: // Transport-level error
                    case 10054: // Transport-level error
                    case 10060: // Network timeout
                    case 40197: // Service busy (Azure)
                    case 40501: // Throttled (Azure)
                    case 40540: // Service crash (Azure)
                    case 40549: // Session terminated (Azure)
                    case 40550: // Lock request (Azure)
                    case 40613: // Database unavailable (Azure)
                    case 49918: // Cannot process (Azure)
                    case 49919: // Process cannot be created (Azure)
                    case 49920: // Limit of workers (Azure)
                        return true;
                }
            }
        }

        if (ex is TimeoutException)
            return true;

        return ex.InnerException != null && IsTransient(ex.InnerException);
    }
}

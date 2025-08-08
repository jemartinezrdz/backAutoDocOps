namespace AutoDocOps.Infrastructure.Helpers;

/// <summary>
/// Helper class for exponential backoff calculations with overflow protection
/// </summary>
internal static class BackoffHelper
{
    /// <summary>
    /// Calculates the next delay using exponential backoff with overflow protection
    /// </summary>
    /// <param name="current">Current delay</param>
    /// <param name="max">Maximum allowed delay</param>
    /// <returns>Next delay value, capped at maximum</returns>
    internal static TimeSpan NextDelay(TimeSpan current, TimeSpan max)
    {
        try
        {
            // Use checked arithmetic to detect overflow
            var nextDelayTicks = checked(current.Ticks * 2);
            return TimeSpan.FromTicks(Math.Min(nextDelayTicks, max.Ticks));
        }
        catch (OverflowException)
        {
            // If overflow occurs, return the maximum delay
            return max;
        }
    }
}

using Microsoft.Extensions.Logging;

namespace AutoDocOps.Infrastructure.Logging;

internal static partial class RedisCacheLogging
{
    [LoggerMessage(EventId = 6000, Level = LogLevel.Debug, Message = "Cache miss for key: {Key}")]
    internal static partial void CacheMiss(this ILogger logger, string key);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Debug, Message = "Cache hit for key: {Key}")]
    internal static partial void CacheHit(this ILogger logger, string key);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Error, Message = "Error getting cached value for key: {Key}")]
    internal static partial void CacheGetError(this ILogger logger, string key, Exception exception);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Debug, Message = "Cached value for key: {Key} with expiration: {Expiration}")]
    internal static partial void CacheSet(this ILogger logger, string key, TimeSpan expiration);

    [LoggerMessage(EventId = 6004, Level = LogLevel.Error, Message = "Error setting cached value for key: {Key}")]
    internal static partial void CacheSetError(this ILogger logger, string key, Exception exception);

    [LoggerMessage(EventId = 6005, Level = LogLevel.Debug, Message = "Removed cached value for key: {Key}")]
    internal static partial void CacheRemove(this ILogger logger, string key);

    [LoggerMessage(EventId = 6006, Level = LogLevel.Error, Message = "Error removing cached value for key: {Key}")]
    internal static partial void CacheRemoveError(this ILogger logger, string key, Exception exception);

    [LoggerMessage(EventId = 6007, Level = LogLevel.Warning, Message = "RemoveByPatternAsync requires IConnectionMultiplexer to be injected. Falling back to no-op. Pattern: {Pattern}")]
    internal static partial void CachePatternNoConnection(this ILogger logger, string pattern);

    [LoggerMessage(EventId = 6008, Level = LogLevel.Information, Message = "Removed {Count} keys matching pattern: {Pattern}")]
    internal static partial void CachePatternRemoved(this ILogger logger, int count, string pattern);

    [LoggerMessage(EventId = 6009, Level = LogLevel.Debug, Message = "No keys found matching pattern: {Pattern}")]
    internal static partial void CachePatternNone(this ILogger logger, string pattern);

    [LoggerMessage(EventId = 6010, Level = LogLevel.Error, Message = "Error removing keys by pattern: {Pattern}")]
    internal static partial void CachePatternError(this ILogger logger, string pattern, Exception exception);

    [LoggerMessage(EventId = 6011, Level = LogLevel.Error, Message = "Error getting cached value synchronously for key: {Key}")]
    internal static partial void CacheGetSyncError(this ILogger logger, string key, Exception exception);
}

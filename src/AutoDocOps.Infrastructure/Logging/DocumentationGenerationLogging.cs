using Microsoft.Extensions.Logging;

namespace AutoDocOps.Infrastructure.Logging;

internal static partial class DocumentationGenerationLogging
{
    [LoggerMessage(EventId = 6100, Level = LogLevel.Information, Message = "Documentation Generation Service started at {StartTime}")]
    internal static partial void DocGenServiceStarted(this ILogger logger, DateTime startTime);

    [LoggerMessage(EventId = 6101, Level = LogLevel.Information, Message = "Documentation Generation Service is shutting down gracefully")]
    internal static partial void DocGenServiceStopping(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 6102, Level = LogLevel.Error, Message = "Critical error in documentation generation service at {ErrorTime}. Retrying after {RetryDelay} (attempt {FailureCount})")]
    internal static partial void DocGenServiceCriticalError(this ILogger logger, DateTime errorTime, TimeSpan retryDelay, int failureCount, Exception exception);

    [LoggerMessage(EventId = 6103, Level = LogLevel.Information, Message = "Documentation Generation Service stopped at {StopTime}")]
    internal static partial void DocGenServiceStopped(this ILogger logger, DateTime stopTime);

    [LoggerMessage(EventId = 6110, Level = LogLevel.Information, Message = "Found {PendingCount} passports to process")]
    internal static partial void DocGenFoundPending(this ILogger logger, int pendingCount);

    [LoggerMessage(EventId = 6111, Level = LogLevel.Information, Message = "Successfully processed passport {PassportId} in {ProcessingTimeMs}ms")]
    internal static partial void DocGenPassportProcessed(this ILogger logger, Guid passportId, long processingTimeMs);

    [LoggerMessage(EventId = 6112, Level = LogLevel.Error, Message = "Failed to process passport {PassportId} after {ProcessingTimeMs}ms: {ErrorMessage}")]
    internal static partial void DocGenPassportError(this ILogger logger, Guid passportId, long processingTimeMs, string errorMessage, Exception exception);

    [LoggerMessage(EventId = 6113, Level = LogLevel.Information, Message = "Processing passport {PassportId} for project {ProjectId}")]
    internal static partial void DocGenProcessingPassport(this ILogger logger, Guid passportId, Guid projectId);

    [LoggerMessage(EventId = 6114, Level = LogLevel.Information, Message = "Completed processing passport {PassportId}")]
    internal static partial void DocGenPassportCompleted(this ILogger logger, Guid passportId);

    [LoggerMessage(EventId = 6115, Level = LogLevel.Debug, Message = "Passport {PassportId}: {Phase}")]
    internal static partial void DocGenPassportPhase(this ILogger logger, Guid passportId, string phase);
}

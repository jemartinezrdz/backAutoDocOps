using Microsoft.Extensions.Logging;

namespace AutoDocOps.WebAPI.Controllers;

internal static partial class GenerateControllerLogs
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Started documentation generation for project {ProjectId}, passport {PassportId}")]
    public static partial void DocumentationStarted(this ILogger logger, Guid projectId, Guid passportId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Invalid documentation generation request for project {ProjectId}")]
    public static partial void InvalidRequest(this ILogger logger, Guid projectId, Exception ex);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Error generating documentation for project {ProjectId}")]
    public static partial void GenerationError(this ILogger logger, Guid projectId, Exception ex);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Retrieving generation status for project {ProjectId}")]
    public static partial void RetrievingStatus(this ILogger logger, Guid projectId);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error, Message = "Error retrieving generation status for project {ProjectId}")]
    public static partial void StatusError(this ILogger logger, Guid projectId, Exception ex);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Cancelling documentation generation for project {ProjectId}")]
    public static partial void CancellingGeneration(this ILogger logger, Guid projectId);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Error, Message = "Error cancelling generation for project {ProjectId}")]
    public static partial void CancelError(this ILogger logger, Guid projectId, Exception ex);
}
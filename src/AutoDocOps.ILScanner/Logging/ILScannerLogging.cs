using Microsoft.Extensions.Logging;

namespace AutoDocOps.ILScanner.Logging;

internal static partial class ILScannerLogging
{
    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Starting analysis for project: {ProjectName}")]
    internal static partial void StartingProjectAnalysis(this ILogger logger, string projectName);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Analysis completed successfully for project: {ProjectName}")]
    internal static partial void ProjectAnalysisCompleted(this ILogger logger, string projectName);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Error, Message = "Error analyzing project: {ProjectName}")]
    internal static partial void ProjectAnalysisError(this ILogger logger, Exception exception, string projectName);

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "Starting SQL analysis for database type: {DatabaseType}")]
    internal static partial void StartingSqlAnalysis(this ILogger logger, string databaseType);

    [LoggerMessage(EventId = 5011, Level = LogLevel.Information, Message = "SQL analysis completed successfully")]
    internal static partial void SqlAnalysisCompleted(this ILogger logger);

    [LoggerMessage(EventId = 5012, Level = LogLevel.Error, Message = "Error analyzing SQL content")]
    internal static partial void SqlAnalysisError(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5020, Level = LogLevel.Warning, Message = "Error analyzing file: {SourceFile}")]
    internal static partial void FileAnalysisWarning(this ILogger logger, Exception exception, string sourceFile);

    [LoggerMessage(EventId = 5030, Level = LogLevel.Error, Message = "Error analyzing SQL content for database type: {DatabaseType}")]
    internal static partial void SqlContentError(this ILogger logger, Exception exception, string databaseType);
}

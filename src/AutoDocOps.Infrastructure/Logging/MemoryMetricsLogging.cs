using Microsoft.Extensions.Logging;

namespace AutoDocOps.Infrastructure.Logging;

internal static partial class MemoryMetricsLogging
{
    [LoggerMessage(EventId = 6300, Level = LogLevel.Debug, Message = "Starting memory monitoring for operation: {OperationName}")]
    internal static partial void MemoryMonitorStart(this ILogger logger, string operationName);

    [LoggerMessage(EventId = 6301, Level = LogLevel.Warning, Message = "High memory usage detected in {OperationName}: {MemoryIncreaseMB:F2} MB")]
    internal static partial void MemoryHighUsage(this ILogger logger, string operationName, double MemoryIncreaseMB);

    [LoggerMessage(EventId = 6302, Level = LogLevel.Warning, Message = "Gen2 garbage collections occurred during {OperationName}: {Collections}")]
    internal static partial void MemoryGen2Collections(this ILogger logger, string operationName, int collections);

    [LoggerMessage(EventId = 6303, Level = LogLevel.Debug, Message = "Memory monitoring completed for {OperationName}: {Delta}")]
    internal static partial void MemoryMonitorCompleted(this ILogger logger, string operationName, string delta);
}

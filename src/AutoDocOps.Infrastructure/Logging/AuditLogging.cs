using Microsoft.Extensions.Logging;

namespace AutoDocOps.Infrastructure.Logging;

internal static partial class AuditLog
{
    [LoggerMessage(EventId = 3200, Level = LogLevel.Warning, Message = "Billing audit channel full - dropping event {EventType}")]
    internal static partial void AuditChannelFull(this ILogger logger, string eventType);

    [LoggerMessage(EventId = 3201, Level = LogLevel.Information, Message = "[AUDIT] {EventType} SecretSource={SecretSource} TraceId={TraceId}")]
    internal static partial void AuditEntryStored(this ILogger logger, string eventType, string secretSource, string? traceId);

    [LoggerMessage(EventId = 3202, Level = LogLevel.Error, Message = "Error writing audit log")]
    internal static partial void AuditWriteError(this ILogger logger, Exception ex);
}

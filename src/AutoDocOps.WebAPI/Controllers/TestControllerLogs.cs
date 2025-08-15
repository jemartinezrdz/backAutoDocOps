namespace AutoDocOps.WebAPI.Controllers;

internal static partial class TestControllerLogs
{
    // Info: llegada al endpoint de test
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Test endpoint hit: {Endpoint} TraceId={TraceId}")]
    public static partial void TestEndpointHit(this ILogger logger, string endpoint, string traceId);

    // Warning: parámetros inválidos o uso indebido del endpoint de test
    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Test endpoint invalid usage: {Endpoint} Reason={Reason}")]
    public static partial void TestEndpointInvalid(this ILogger logger, string endpoint, string reason);

    // Error: fallo procesando el endpoint
    [LoggerMessage(EventId = 2003, Level = LogLevel.Error,
        Message = "Test endpoint failed: {Endpoint}")]
    public static partial void TestEndpointFailed(this ILogger logger, string endpoint, Exception ex);
}
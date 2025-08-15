using Microsoft.Extensions.Logging;

namespace AutoDocOps.Infrastructure.Logging;

internal static partial class OpenAiLogging
{
    [LoggerMessage(EventId = 6200, Level = LogLevel.Warning, Message = "API key validation failed: {Reason}")]
    internal static partial void OpenAiApiKeyInvalid(this ILogger logger, string reason);

    [LoggerMessage(EventId = 6201, Level = LogLevel.Error, Message = "Error during chat completion for query: {Query}")]
    internal static partial void OpenAiChatError(this ILogger logger, string query, Exception exception);
}

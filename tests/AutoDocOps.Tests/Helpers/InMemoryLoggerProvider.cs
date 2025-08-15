using Microsoft.Extensions.Logging;

namespace AutoDocOps.Tests.Helpers;

public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    public List<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(Entries);
    public void Dispose() { }
    
    public List<string> GetLogMessages() => Entries.Select(e => e.Message).ToList();

    private sealed class InMemoryLogger : ILogger
    {
        private readonly List<LogEntry> _sink;
        public InMemoryLogger(List<LogEntry> sink) => _sink = sink;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _sink.Add(new LogEntry(logLevel, eventId, state ?? (object)"null", exception, formatter(state, exception)));
        }
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}

public readonly record struct LogEntry(LogLevel Level, EventId EventId, object State, Exception? Exception, string Message);
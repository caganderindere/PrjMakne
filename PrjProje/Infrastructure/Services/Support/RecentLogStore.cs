using System.Collections.Concurrent;
using PrjProje.Application.Models.Admin;
using Microsoft.Extensions.Logging;

namespace PrjProje.Infrastructure.Services.Support;

/// <summary>Keeps the latest warnings and errors so the admin Developer page can show them.</summary>
// shortcut: memory only, lost on restart and per server instance; move to a table or file sink if history must survive deploys.
public sealed class RecentLogStore : ILoggerProvider
{
    private const int Capacity = 200;
    private const int MaxTextLength = 8000;

    private readonly ConcurrentQueue<RecentLogEntry> _entries = new();

    public ILogger CreateLogger(string categoryName) => new StoreLogger(this, categoryName);

    /// <summary>Newest first.</summary>
    public IReadOnlyList<RecentLogEntry> Snapshot() => _entries.Reverse().ToList();

    public void Dispose()
    {
    }

    private void Add(RecentLogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > Capacity)
        {
            _entries.TryDequeue(out _);
        }
    }

    private static string Limit(string text) => text.Length > MaxTextLength ? text[..MaxTextLength] : text;

    private sealed class StoreLogger : ILogger
    {
        private readonly RecentLogStore _store;
        private readonly string _category;

        public StoreLogger(RecentLogStore store, string category)
        {
            _store = store;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel is >= LogLevel.Warning and < LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _store.Add(new RecentLogEntry(
                    DateTime.UtcNow,
                    logLevel >= LogLevel.Error,
                    _category,
                    Limit(formatter(state, exception)),
                    exception is null ? null : Limit(exception.ToString())));
            }
        }
    }
}

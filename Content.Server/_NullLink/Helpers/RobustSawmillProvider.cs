using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using LogLevel = Robust.Shared.Log.LogLevel;
using LogLevelNet = Microsoft.Extensions.Logging.LogLevel;

namespace Content.Server._NullLink.Helpers;
/// <summary>Bridges Microsoft.Extensions.Logging to Robust.Sawmill.</summary>
public sealed class RobustSawmillProvider(ISawmill root) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, RobustSawmillLogger> _cache = new();

    public ILogger CreateLogger(string categoryName)
        => _cache.GetOrAdd(categoryName, name => new RobustSawmillLogger(root, name));

    public void Dispose() { }

    private sealed class RobustSawmillLogger(ISawmill root, string category) : ILogger
    {
        public bool IsEnabled(LogLevelNet level)
            => root.IsLogLevelEnabled(Map(level));

        public void Log<TState>(LogLevelNet level,
                                EventId _,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;

            var msg = $"[{category}] {formatter(state, exception)}";
            if (exception != null)
                root.Log(Map(level), exception, msg);
            else
                root.Log(Map(level), msg);
        }

        private static LogLevel Map(LogLevelNet lvl) => lvl switch
        {
            LogLevelNet.Trace => LogLevel.Verbose,
            LogLevelNet.Debug => LogLevel.Debug,
            LogLevelNet.Information => LogLevel.Info,
            LogLevelNet.Warning => LogLevel.Warning,
            LogLevelNet.Error => LogLevel.Error,
            LogLevelNet.Critical => LogLevel.Fatal,
            _ => LogLevel.Info
        };

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;
    }
}
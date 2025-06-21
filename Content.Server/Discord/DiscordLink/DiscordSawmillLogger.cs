using NetCord.Logging;
using NLogLevel = NetCord.Logging.LogLevel;
using LogLevel = Robust.Shared.Log.LogLevel;

namespace Content.Server.Discord.DiscordLink;

public sealed class DiscordSawmillLogger(ISawmill sawmill) : IGatewayLogger, IRestLogger, IVoiceLogger
{
    private static LogLevel GetLogLevel(NLogLevel logLevel)
    {
        return logLevel switch
        {
            NLogLevel.Critical => LogLevel.Fatal,
            NLogLevel.Error => LogLevel.Error,
            NLogLevel.Warning => LogLevel.Warning,
            _ => LogLevel.Debug,
        };
    }

    void IGatewayLogger.Log<TState>(NetCord.Logging.LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        sawmill.Log(GetLogLevel(logLevel), exception, formatter(state, exception));
    }

    void IRestLogger.Log<TState>(NetCord.Logging.LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        sawmill.Log(GetLogLevel(logLevel), exception, formatter(state, exception));
    }

    void IVoiceLogger.Log<TState>(NetCord.Logging.LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        sawmill.Log(GetLogLevel(logLevel), exception, formatter(state, exception));
    }
}

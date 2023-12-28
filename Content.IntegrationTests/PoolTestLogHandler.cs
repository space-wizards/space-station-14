using System.IO;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.IntegrationTests;

#nullable enable

/// <summary>
/// Log handler intended for pooled integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This class logs to two places: an NUnit <see cref="TestContext"/>
/// (so it nicely gets attributed to a test in your IDE),
/// and an in-memory ring buffer for diagnostic purposes.
/// If test pooling breaks, the ring buffer can be used to see what the broken instance has gone through.
/// </para>
/// <para>
/// The active test context can be swapped out so pooled instances can correctly have their logs attributed.
/// </para>
/// </remarks>
public sealed class PoolTestLogHandler : ILogHandler
{
    private readonly string? _prefix;

    private RStopwatch _stopwatch;

    public TextWriter? ActiveContext { get; private set; }

    public LogLevel? FailureLevel { get; set; }

    public PoolTestLogHandler(string? prefix)
    {
        _prefix = prefix != null ? $"{prefix}: " : "";
    }

    public bool ShuttingDown;

    public void Log(string sawmillName, LogEvent message)
    {
        var level = message.Level.ToRobust();

        if (ShuttingDown && (FailureLevel == null || level < FailureLevel))
            return;

        if (ActiveContext is not { } testContext)
        {
            // If this gets hit it means something is logging to this instance while it's "between" tests.
            // This is a bug in either the game or the testing system, and must always be investigated.
            throw new InvalidOperationException("Log to pool test log handler without active test context");
        }

        var name = LogMessage.LogLevelToName(level);
        var seconds = _stopwatch.Elapsed.TotalSeconds;
        var rendered = message.RenderMessage();
        var line = $"{_prefix}{seconds:F3}s [{name}] {sawmillName}: {rendered}";

        testContext.WriteLine(line);

        if (FailureLevel == null || level < FailureLevel)
            return;

        testContext.Flush();
        Assert.Fail($"{line} Exception: {message.Exception}");
    }

    public void ClearContext()
    {
        ActiveContext = null;
    }

    public void ActivateContext(TextWriter context)
    {
        _stopwatch.Restart();
        ActiveContext = context;
    }
}

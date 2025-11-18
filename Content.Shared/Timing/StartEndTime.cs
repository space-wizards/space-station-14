using Robust.Shared.Timing;

namespace Content.Shared.Timing;

/// <summary>
/// Represents a range of an "action" in time, as start/end times.
/// </summary>
/// <remarks>
/// Positions in time are represented as <see cref="TimeSpan"/>s, usually from <see cref="IGameTiming.CurTime"/>
/// or <see cref="IGameTiming.RealTime"/>.
/// </remarks>
/// <param name="Start">The time the action starts.</param>
/// <param name="End">The time action ends.</param>
[Serializable]
public record struct StartEndTime(TimeSpan Start, TimeSpan End)
{
    /// <summary>
    /// How long the action takes.
    /// </summary>
    public TimeSpan Length => End - Start;

    /// <summary>
    /// Get how far the action has progressed relative to a time value.
    /// </summary>
    /// <param name="time">The time to get the current progress value for.</param>
    /// <param name="clamp">If true, clamp values outside the time range to 0 through 1.</param>
    /// <returns>
    /// <para>
    /// A progress value. Zero means <paramref name="time"/> is at <see cref="Start"/>,
    /// one means <paramref name="time"/> is at <see cref="End"/>.
    /// </para>
    /// <para>
    /// This function returns <see cref="float.NaN"/> if <see cref="Start"/> and <see cref="End"/> are identical.
    /// </para>
    /// </returns>
    public float ProgressAt(TimeSpan time, bool clamp = true)
    {
        var length = Length;
        if (length == default)
            return float.NaN;

        var progress = (float) ((time - Start) / length);
        if (clamp)
            progress = MathHelper.Clamp01(progress);

        return progress;
    }

    public static StartEndTime FromStartDuration(TimeSpan start, TimeSpan duration)
    {
        return new StartEndTime(start, start + duration);
    }

    public static StartEndTime FromStartDuration(TimeSpan start, float durationSeconds)
    {
        return new StartEndTime(start, start + TimeSpan.FromSeconds(durationSeconds));
    }

    public static StartEndTime FromCurTime(IGameTiming gameTiming, TimeSpan duration)
    {
        return FromStartDuration(gameTiming.CurTime, duration);
    }

    public static StartEndTime FromCurTime(IGameTiming gameTiming, float durationSeconds)
    {
        return FromStartDuration(gameTiming.CurTime, durationSeconds);
    }
}

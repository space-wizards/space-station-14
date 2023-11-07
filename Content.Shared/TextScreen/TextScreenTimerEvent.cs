namespace Content.Shared.TextScreen.Events;

/// <summary>
/// Sets the Duration on a TextScreen, and sets its display mode to Timer, counting down to zero.
/// </summary>
[ByRefEvent]
public readonly record struct TextScreenTimerEvent(TimeSpan?[] Duration)
{
    public readonly TimeSpan?[] Duration = Duration;
}

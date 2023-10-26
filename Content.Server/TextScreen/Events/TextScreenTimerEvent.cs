namespace Content.Server.TextScreen.Events;

[ByRefEvent]
public readonly record struct TextScreenTimerEvent(TimeSpan Duration)
{
    public readonly TimeSpan Duration = Duration;
}

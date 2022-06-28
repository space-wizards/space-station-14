using Content.Server.Shuttles.Components;

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised on a <see cref="ShuttleConsoleComponent"/> when it's trying to get its shuttle console to pilot.
/// </summary>
[ByRefEvent]
public struct ConsoleShuttleEvent
{
    public EntityUid? Console;
}

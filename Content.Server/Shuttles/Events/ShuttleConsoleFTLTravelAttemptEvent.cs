using Content.Server.Shuttles.Components;

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when shuttle console tries to FTL to other map. Can be cancelled.
/// </summary>
[ByRefEvent]
public struct ShuttleConsoleFTLTravelAttemptEvent
{
    public bool Cancelled = false;
    public EntityUid Uid;
    public ShuttleConsoleComponent Component;
    public EntityUid Destination;
    public string? Reason;
    public ShuttleConsoleFTLTravelAttemptEvent(EntityUid uid, ShuttleConsoleComponent component, EntityUid dest)
    {
        Uid = uid;
        Component = component;
        Destination = dest;
    }
}
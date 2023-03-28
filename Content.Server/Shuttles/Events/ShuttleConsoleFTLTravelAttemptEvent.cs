using Content.Server.Shuttles.Components;

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when shuttle console tries to FTL to other map. Can be cancelled.
/// </summary>
[ByRefEvent]
public record struct ShuttleConsoleFTLTravelAttemptEvent
{
    public bool Cancelled = false;
    public EntityUid Uid;
    public string? Reason;
    public ShuttleConsoleFTLTravelAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
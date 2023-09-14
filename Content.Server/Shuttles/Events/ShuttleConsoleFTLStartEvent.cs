namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when shuttle console approved FTL
/// </summary>
[ByRefEvent]
public record struct ShuttleConsoleFTLTravelStartEvent(EntityUid Uid)
{
    public EntityUid Uid = Uid;
}

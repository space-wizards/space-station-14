namespace Content.Shared.Body.Events;

[ByRefEvent]
public record struct ShiverAttemptEvent(EntityUid Uid)
{
    public readonly EntityUid Uid = Uid;
    public bool Cancelled = false;
}

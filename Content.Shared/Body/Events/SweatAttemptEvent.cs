namespace Content.Shared.Body.Events;

[ByRefEvent]
public record struct SweatAttemptEvent(EntityUid Uid)
{
    public readonly EntityUid Uid = Uid;
    public bool Cancelled = false;
}

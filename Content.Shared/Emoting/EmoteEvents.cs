namespace Content.Shared.Emoting;

public sealed class EmoteAttemptEvent(EntityUid uid) : CancellableEntityEventArgs
{
    public EntityUid Uid { get; } = uid;
}

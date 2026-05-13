namespace Content.Shared.Emoting;

public sealed partial class EmoteAttemptEvent(EntityUid uid) : CancellableEntityEventArgs
{
    public EntityUid Uid { get; } = uid;
}


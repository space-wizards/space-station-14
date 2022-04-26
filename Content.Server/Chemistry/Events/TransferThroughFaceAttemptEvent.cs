namespace Content.Server.Chemistry.Events;

public sealed class TransferThroughFaceAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid Uid { get; }

    public TransferThroughFaceAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

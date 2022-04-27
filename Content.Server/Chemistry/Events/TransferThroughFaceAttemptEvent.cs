namespace Content.Server.Chemistry.Events;

public sealed class TransferThroughFaceAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The Target that is going to have the reagent transferred to
    /// </summary>
    public EntityUid Uid { get; }

    public TransferThroughFaceAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

namespace Content.Shared.Silicons.Borgs;

[ByRefEvent]
public sealed class AttemptMakeBrainIntoSiliconEvent : CancellableEntityEventArgs
{
    public EntityUid BrainHolder;

    public AttemptMakeBrainIntoSiliconEvent(EntityUid brainHolder)
    {
        BrainHolder = brainHolder;
    }
}

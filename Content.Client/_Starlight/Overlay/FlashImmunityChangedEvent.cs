namespace Content.Client._Starlight.Overlay;

public sealed class FlashImmunityChangedEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;
    public readonly bool IsImmune;

    public FlashImmunityChangedEvent(EntityUid entityUid, bool isImmune)
    {
        EntityUid = entityUid;
        IsImmune = isImmune;
    }
}

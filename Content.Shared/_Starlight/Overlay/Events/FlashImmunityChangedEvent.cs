namespace Content.Shared.Starlight.Overlay;

public sealed class FlashImmunityCheckEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;
    public readonly bool IsImmune;

    public FlashImmunityCheckEvent(EntityUid entityUid, bool isImmune)
    {
        EntityUid = entityUid;
        IsImmune = isImmune;
    }
}

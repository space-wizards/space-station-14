namespace Content.Server._Starlight.Medical.Limbs;

[ByRefEvent]
public record struct LimbAttachedEvent<T>
{
    public LimbAttachedEvent(EntityUid limb, T comp)
    {
        Limb = limb;
        Comp = comp;
    }
    public readonly EntityUid Limb;
    public readonly T Comp;
}
    [ByRefEvent]
public record struct LimbRemovedEvent<T>
{
    public LimbRemovedEvent(EntityUid limb, T comp)
    {
        Limb = limb;
        Comp = comp;
    }
    public readonly EntityUid Limb;
    public readonly T Comp;
}
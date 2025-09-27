namespace Content.Server._Starlight.Medical.Limbs;

[ByRefEvent]
public record struct LimbAttachedEvent
{
    public EntityUid Limb;
    public EntityUid Body;
}

[ByRefEvent]
public record struct LimbPreDetachEvent
{
    public EntityUid Limb;
    public EntityUid Body;
}

[ByRefEvent]
public record struct LimbDetachedEvent
{
    public EntityUid Limb;
    public EntityUid Body;
}
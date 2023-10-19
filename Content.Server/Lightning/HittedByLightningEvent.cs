namespace Content.Server.Lightning.Events;

/// <summary>
/// Called when lightning bolt collide with a entity
/// </summary>
[ByRefEvent]
public readonly struct HittedByLightningEvent
{
    public readonly EntityUid Lightning;
    public readonly EntityUid Target;

    public HittedByLightningEvent(EntityUid lightning, EntityUid target)
    {
        Lightning = lightning;
        Target = target;
    }
}

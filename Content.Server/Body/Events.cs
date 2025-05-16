using Content.Server.Body.Components;

[ByRefEvent]
public record struct BloodColorOverrideEvent()
{
    /// <summary>
    /// The entity getting the appearance component.
    /// </summary>
    public EntityUid Owner;

    /// <summary>
    /// The apearance component that was modified.
    /// </summary>
    public BloodstreamComponent? BloodstreamComp;
}

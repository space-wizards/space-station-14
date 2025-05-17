using Content.Server.Body.Components;

/// <summary>
/// Subscribe to this event and set the
/// BloodOverrideColor to override blood
/// reagent color
/// </summary>
[ByRefEvent]
public record struct BloodColorOverrideEvent()
{
    public EntityUid Owner;
    public BloodstreamComponent? BloodstreamComp;
}

namespace Content.Shared.Radio.Components;

[RegisterComponent]
public sealed partial class RelayComponent : Component
{
    [DataField("isActive")]
    public bool IsActive = true; // Indicates if the relay is currently active.

    public bool BoostsLongRange => IsActive; // When active, boosts all channels to LongRange.
}

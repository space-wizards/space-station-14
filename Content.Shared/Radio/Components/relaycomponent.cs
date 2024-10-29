[RegisterComponent]
public sealed class RelayComponent : Component
{
    [DataField("isActive")]
    public bool IsActive = true; // Indicates if the relay is currently active.

    public bool BoostsLongRange => IsActive; // When active, boosts all channels to LongRange.
}

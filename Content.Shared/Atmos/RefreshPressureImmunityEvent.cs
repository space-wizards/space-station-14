namespace Content.Shared.Atmos;

/// <summary>
/// Raised on an entity to refresh whether it is currently immune to pressure damage.
/// </summary>
[ByRefEvent]
public record struct RefreshPressureImmunityEvent
{
    public bool IsImmune;
}

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised to try and get any tile friction modifiers for a particular body.
/// </summary>
[ByRefEvent]
public struct TileFrictionEvent
{
    public float Modifier;

    public TileFrictionEvent(float modifier)
    {
        // TODO: If something ever uses different angular and linear modifiers, split this into two modifiers
        Modifier = modifier;
    }
}

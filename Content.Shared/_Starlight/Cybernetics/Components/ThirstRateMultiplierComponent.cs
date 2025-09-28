namespace Content.Shared.Starlight.Cybernetics.Components;

/// <summary>
/// This component increases thirst rate of an entity with thirst component
/// </summary>
[RegisterComponent]
public sealed partial class ThirstRateMultiplierComponent : Component
{
    [DataField]
    public float Multiplier = 1f;
}

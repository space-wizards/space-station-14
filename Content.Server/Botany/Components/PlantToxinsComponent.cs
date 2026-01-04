using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

/// <summary>
/// Data for plant resistance to toxins.
/// </summary>
[RegisterComponent]
[DataDefinition]
[Access(typeof(PlantToxinsSystem))]
public sealed partial class PlantToxinsComponent : Component
{
    /// <summary>
    /// Maximum toxin level the plant can tolerate before taking damage.
    /// </summary>
    [DataField]
    public float ToxinsTolerance = 4f;

    /// <summary>
    /// Divisor for calculating toxin uptake rate. Higher values mean slower toxin processing.
    /// </summary>
    [DataField]
    public float ToxinUptakeDivisor = 10f;
}

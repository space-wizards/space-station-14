namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class ToxinsComponent : PlantGrowthComponent
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

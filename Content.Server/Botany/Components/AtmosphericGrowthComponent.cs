namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class AtmosphericGrowthComponent : PlantGrowthComponent
{
    /// <summary>
    /// Ideal temperature for plant growth in Kelvin.
    /// </summary>
    [DataField]
    public float IdealHeat = 293f;

    /// <summary>
    /// Temperature tolerance range around ideal heat.
    /// </summary>
    [DataField]
    public float HeatTolerance = 10f;

    /// <summary>
    /// Minimum pressure tolerance for plant growth.
    /// </summary>
    [DataField]
    public float LowPressureTolerance = 81f;

    /// <summary>
    /// Maximum pressure tolerance for plant growth.
    /// </summary>
    [DataField]
    public float HighPressureTolerance = 121f;
}

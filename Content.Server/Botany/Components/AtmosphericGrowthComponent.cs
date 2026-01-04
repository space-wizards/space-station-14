using Content.Shared.Atmos;
using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

/// <summary>
/// Atmospheric-related requirements for proper entity growth. Used in botany.
/// </summary>
[RegisterComponent]
[DataDefinition]
[Access(typeof(AtmosphericGrowthSystem))]
public sealed partial class AtmosphericGrowthComponent : Component
{
    /// <summary>
    /// Ideal temperature for plant growth in Kelvin.
    /// </summary>
    [DataField]
    public float IdealHeat = Atmospherics.T20C;

    /// <summary>
    /// Temperature tolerance range around <see cref="IdealHeat"/>.
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

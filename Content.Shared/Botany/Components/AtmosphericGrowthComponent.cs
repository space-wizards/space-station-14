using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Atmospheric-related requirements for proper entity growth. Used in botany.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAtmosphericGrowthSystem))]
public sealed partial class AtmosphericGrowthComponent : Component
{
    /// <summary>
    /// Damage per unit of heat tolerance exceeded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatToleranceDamage = 2f;

    /// <summary>
    /// Minimum temperature tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowHeatTolerance = 283f; // 10°C

    /// <summary>
    /// Maximum temperature tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighHeatTolerance = 303f; // 30°C

    /// <summary>
    /// Damage per unit of pressure tolerance exceeded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PressureToleranceDamage = 2f;

    /// <summary>
    /// Minimum pressure tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowPressureTolerance = 81f; // 101 kPa

    /// <summary>
    /// Maximum pressure tolerance for plant growth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighPressureTolerance = 121f; // 141 kPa
}

using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for atmospheric-related requirements for proper plant growth.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedPlantAtmosphericSystem))]
public sealed partial class PlantAtmosphericComponent : Component
{
    /// <summary>
    /// The range a plant needs to be outside it's ideal temperatures to take the standard amount of
    /// damage (HeatToleranceDamage).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatToleranceDifference = 20f;

    /// <summary>
    /// Damage taken per growth cycle at exactly HeatToleranceDifference degrees above or below the plant's heat
    /// thresholds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatToleranceDamage = 2f;

    /// <summary>
    /// Lower steepness increases plant damage taken at a high temperature differentials and decreases damage at low
    /// temperature differentials.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatToleranceSteepness = 1f;

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
    /// The amount a plant needs to be outside it's ideal pressure range to take the standard amount of
    /// damage (PressureToleranceDamage).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PressureToleranceDifference = 10f;

    /// <summary>
    /// Damage taken per growth cycle at exactly PressureToleranceDifference kPa above or below the plant's pressure
    /// thresholds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PressureToleranceDamage = 2f;

    /// <summary>
    /// Lower steepness increases plant damage taken at a high pressure differentials and decreases damage at low
    /// pressure differentials.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PressureToleranceSteepness = 1f;

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

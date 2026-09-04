using Content.Shared.Atmos;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Generic temperature controller with separate heating and cooling power.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedThermoregulatorSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ThermoregulatorComponent : Component, IHeatContainer
{
    /// <summary>
    /// The heat capacity of the thermoregulator in joules per kelvin.
    /// </summary>
    [DataField]
    public float HeatCapacity { get; set; } = 500f;

    /// <inheritdoc/>
    [DataField, AutoNetworkedField]
    public float Temperature { get; set; } = Atmospherics.T20C;

    /// <summary>
    /// Interval between simulation updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next scheduled simulation update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Current heating or cooling state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ThermoregulatorActiveMode ActiveMode = ThermoregulatorActiveMode.Idle;

    /// <summary>
    /// Allowed direction of temperature control.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ThermoregulatorMode Mode = ThermoregulatorMode.Auto;

    /// <summary>
    /// Target temperature setpoint in Kelvin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Setpoint = Atmospherics.T20C;

    /// <summary>
    /// Temperature difference required to start regulating, in kelvin.
    /// Once active, the regulator runs until it reaches the setpoint.
    /// </summary>
    [DataField]
    public float TemperatureTolerance = 0.05f;

    /// <summary>
    /// Maximum allowed temperature setpoint.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float MaxTemperature = 573.15f; // 300 °C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Minimum allowed temperature setpoint.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float MinTemperature = 253.15f; // -20 °C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Heating power in watts.
    /// </summary>
    [DataField]
    public float HeatingPower = 200f;

    /// <summary>
    /// Cooling power in watts.
    /// </summary>
    [DataField]
    public float CoolingPower = 60f;

    /// <summary>
    /// Thermal conductance between the regulator and the controlled object, in watts per kelvin.
    /// </summary>
    [DataField]
    public float ThermalConductance = 2f;
}

/// <summary>
/// Directions in which a thermoregulator is allowed to operate.
/// </summary>
[Serializable, NetSerializable]
public enum ThermoregulatorMode : byte
{
    /// <summary>Only remove heat.</summary>
    Cooling = 0,

    /// <summary>Add or remove heat as needed.</summary>
    Auto = 1,

    /// <summary>Only add heat.</summary>
    Heating = 2
}

/// <summary>
/// The direction in which a thermoregulator is currently operating.
/// </summary>
[Serializable, NetSerializable]
public enum ThermoregulatorActiveMode : byte
{
    /// <summary>No heat is currently being added or removed.</summary>
    Idle = 0,

    /// <summary>Heat is currently being removed.</summary>
    Cooling = 1,

    /// <summary>Heat is currently being added.</summary>
    Heating = 2
}

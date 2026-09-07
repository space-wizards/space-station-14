using Content.Shared.Atmos;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Generic implementation of a hysteresis-based temperature controller.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedThermoregulatorSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ThermoregulatorComponent : Component, IHeatContainer
{
    /// <summary>
    /// Whether the thermoregulator is enabled.
    /// </summary>
    [DataField]
    public float HeatCapacity { get; set; } = 500f;

    /// <inheritdoc/>
    [DataField, AutoNetworkedField]
    public float Temperature { get; set; } = Atmospherics.T20C;

    /// <summary>
    /// The <see cref="TimeSpan"/> interval between updates of the controller.
    /// </summary>
    /// <remarks>
    /// The temperature change every tick will be very small with the default settings (0.006 K),
    /// so we don't want to run this so often. If your heating/cooling power is much higher than the default,
    /// you might want to tune this.
    /// </remarks>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The <see cref="TimeSpan"/> of the next update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Current active state of the thermoregulator.
    /// <seealso cref="ThermoregulatorActiveMode"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public ThermoregulatorActiveMode ActiveMode = ThermoregulatorActiveMode.Idle;

    /// <summary>
    /// Operation mode of the thermoregulator.
    /// <seealso cref="ThermoregulatorMode"/>
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
    [DataField]
    public float MaxTemperature = 573.15f; // 300 °C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Minimum allowed temperature setpoint.
    /// </summary>
    [DataField]
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

[Serializable, NetSerializable]
public enum ThermoregulatorMode : byte
{
    Cooling = 0,
    Auto = 1,
    Heating = 2
}

[Serializable, NetSerializable]
public enum ThermoregulatorActiveMode : byte
{
    Idle = 0,
    Cooling = 1,
    Heating = 2
}

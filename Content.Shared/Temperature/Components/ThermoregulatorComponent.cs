using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Generic implementation of a hysteresis-based temperature controller.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ThermoregulatorSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ThermoregulatorComponent : Component
{
    /// <summary>
    /// Whether the thermoregulator is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Current temperature in Kelvin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature = 293.15f; // TODO: This should be on TemperatureComponent when it's networked and generic

    /// <summary>
    /// Heat capacity of the thermoregulator in J/K.
    /// </summary>
    /// <remarks>
    /// This determines how quickly temperature changes in response to heating/cooling.
    /// It represents the heat capacity of whatever heating/cooling element there is.
    /// </remarks>
    [DataField]
    public float HeatCapacity = 500f; // TODO: This should be on TemperatureComponent when it's networked and generic

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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
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
    public float Setpoint = 293.15f;

    /// <summary>
    /// Maximum allowed temperature setpoint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 573.15f; // 300C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Minimum allowed temperature setpoint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinTemperature = 253.15f; // -20C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Temperature hysteresis in Kelvin.
    /// </summary>
    [DataField]
    public float Hysteresis = 1.5f;

    /// <summary>
    /// Power scaling band beyond hysteresis.
    /// </summary>
    [DataField]
    public float ScaleBand = 6f;

    /// <summary>
    /// Heating power in watts.
    /// </summary>
    [DataField]
    public float HeatingPower = 100f;

    /// <summary>
    /// Cooling power in watts.
    /// </summary>
    [DataField]
    public float CoolingPower = 30f;
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

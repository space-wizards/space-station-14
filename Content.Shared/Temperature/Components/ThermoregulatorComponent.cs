using Content.Shared.Atmos;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Generic heat container that regulates its temperature using device-provided limits and target.
/// </summary>
[RegisterComponent, NetworkedComponent]
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
    /// The <see cref="TimeSpan"/> interval between updates of the controller.
    /// </summary>
    /// <remarks>
    /// Use a shorter interval for regulators with enough power to produce large temperature changes per update.
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
    /// Temperature difference required to start regulating, in kelvin.
    /// Once active, the regulator runs until it reaches the target temperature.
    /// </summary>
    [DataField]
    public float TemperatureTolerance = 0.05f;

    /// <summary>
    /// Thermal conductance between the regulator and the controlled object, in watts per kelvin.
    /// </summary>
    [DataField]
    public float ThermalConductance = 2f;
}

[Serializable, NetSerializable]
public enum ThermoregulatorActiveMode : byte
{
    Idle = 0,
    Cooling = 1,
    Heating = 2
}

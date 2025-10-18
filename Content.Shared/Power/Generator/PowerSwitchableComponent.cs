using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Enables a device to switch between HV, MV and LV connectors.
/// For generators this means changing output wires.
/// </summary>
/// <remarks>
/// Must have <c>CableDeviceNode</c>s for each output in <see cref="Outputs"/>.
/// If its a generator <c>PowerSupplierComponent</c> is also required.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPowerSwitchableSystem))]
public sealed partial class PowerSwitchableComponent : Component
{
    /// <summary>
    /// Index into <see cref="Cables"/> that the device is currently using.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ActiveIndex;

    /// <summary>
    /// Sound that plays when the cable is switched.
    /// </summary>
    [DataField]
    public SoundSpecifier? SwitchSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    /// Locale id for text shown when examined.
    /// It is given "voltage" as a colored voltage string.
    /// </summary>
    [DataField(required: true)]
    public string ExamineText = string.Empty;

    /// <summary>
    /// Locale id for the popup shown when switching voltages.
    /// It is given "voltage" as a colored voltage string.
    /// </summary>
    [DataField(required: true)]
    public string SwitchText = string.Empty;

    /// <summary>
    /// Cable voltages and their nodes which can be cycled between.
    /// Each node name must match a cable node in its <c>NodeContainer</c>.
    /// </summary>
    [DataField(required: true)]
    public List<PowerSwitchableCable> Cables = new();
}

/// <summary>
/// Cable voltage and node name for cycling.
/// </summary>
[DataDefinition]
public sealed partial class PowerSwitchableCable
{
    /// <summary>
    /// Voltage that the cable uses.
    /// </summary>
    [DataField(required: true)]
    public SwitchableVoltage Voltage;

    /// <summary>
    /// Name of the node for the cable.
    /// Must be a <c>CableDeviceNode</c>
    /// </summary>
    [DataField(required: true)]
    public string Node = string.Empty;
}

/// <summary>
/// Cable voltage to cycle between.
/// </summary>
[Serializable, NetSerializable]
public enum SwitchableVoltage : byte
{
    HV,
    MV,
    LV
}

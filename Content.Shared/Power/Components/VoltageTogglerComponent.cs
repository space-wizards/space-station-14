using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Changes the voltage of a device that has some field related to voltage
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VoltageTogglerComponent : Component
{
    /// <summary>
    /// List of all voltage settings.
    /// </summary>
    [DataField(required: true)]
    public VoltageSetting[] Settings = [];

    /// <summary>
    /// Index of the currently selected setting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SelectedVoltageLevel;

    /// <summary>
    /// Locale id for the popup shown when switching voltages.
    /// It is given "voltage" as a colored voltage string.
    /// </summary>
    [DataField]
    public LocId? SwitchText;

    /// <summary>
    /// Locale id for text shown when examined.
    /// It is given "voltage" as a colored voltage string.
    /// </summary>
    [DataField]
    public LocId ExamineText = "voltage-toggler-examine";

    /// <summary>
    /// Sound that plays when the cable is switched.
    /// </summary>
    [DataField]
    public SoundSpecifier? SwitchSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}

/// <summary>
/// A voltage setting from which a device can be set to change to.
/// </summary>
[DataDefinition]
public partial struct VoltageSetting
{
    /// <summary>
    /// The voltage of the setting
    /// </summary>
    [DataField(required: true)]
    public Voltage Voltage;

    /// <summary>
    /// Name of the node for the cable.
    /// Must be a <c>CableDeviceNode</c>
    /// </summary>
    [DataField(required: true)]
    public string Node = string.Empty;

    /// <summary>
    /// Name of the setting.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}

/// <summary>
/// Event called when the voltage of a device with <see cref="VoltageTogglerComponent"/> is toggled
/// </summary>
/// <param name="NewVoltage">the new voltage setting</param>
[ByRefEvent]
public record struct VoltageChangeEvent(VoltageSetting NewVoltage);

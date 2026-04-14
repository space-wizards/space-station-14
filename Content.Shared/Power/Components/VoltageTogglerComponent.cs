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
    /// Power usage in that voltage.
    /// </summary>
    /// <remarks>If null it doesn't change any power use</remarks>
    [DataField]
    public float? Wattage = null;

    /// <summary>
    /// Name of the setting.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}

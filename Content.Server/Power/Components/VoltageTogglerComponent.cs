using Robust.Shared.GameStates;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

/// <summary>
///     Changes the voltage of a device with <see cref="PowerConsumerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoltageTogglerComponent : Component
{
    /// <summary>
    /// List of all voltage settings.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public VoltageSetting[] Settings = [];

    /// <summary>
    /// Index of the currently selected setting.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int SelectedVoltageLevel;
}

[DataDefinition]
public partial struct VoltageSetting
{
    /// <summary>
    /// Voltage.
    /// </summary>
    [DataField(required: true)]
    public Voltage Voltage;

    /// <summary>
    /// Power usage in that voltage.
    /// </summary>
    [DataField(required: true)]
    public float Wattage;

    /// <summary>
    /// Name of the setting.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}

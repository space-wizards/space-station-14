namespace Content.Shared.Power.Components;

/// <summary>
///     Changes the voltage of a device with <see cref="PowerConsumerComponent"/>
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
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
    [DataField]
    [AutoNetworkedField]
    public int SelectedVoltageLevel;
}

[DataDefinition]
public partial struct VoltageSetting
{
    /// <summary>
    /// The voltage of the setting,
    /// that being which cable type the entity with <see cref="PowerConsumerComponent"/> will consume power from.
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

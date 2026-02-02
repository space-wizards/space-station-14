using Content.Shared.Power;

namespace Content.Server.Power.Components;

/// <summary>
///     Changes the voltage of a device with <see cref="PowerConsumerComponent"/>
/// </summary>
public sealed partial class VoltageTogglerComponent : Component
{
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
        public LocId Name = string.Empty;
    }

    /// <summary>
    /// List of all voltage settings.
    /// </summary>
    [DataField]
    public VoltageSetting[] Settings;

    /// <summary>
    /// Index of the currently selected setting.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int SelectedVoltageLevel;
}

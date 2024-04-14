using System.ComponentModel;
using Content.Server.Radio.EntitySystems;
using Content.Shared.RadioJammer;
using Robust.Shared.GameStates;

namespace Content.Server.Radio.Components;

/// <summary>
/// When activated (<see cref="ActiveRadioJammerComponent"/>) prevents from sending messages in range
/// Suit sensors will also stop working.
/// </summary>
[NetworkedComponent, RegisterComponent]
[Access(typeof(JammerSystem))]
public sealed partial class RadioJammerComponent : SharedRadioJammerComponent
{
    [DataDefinition]
    public partial struct RadioJamSetting
    {
        /// <summary>
        /// Power usage per second when enabled.
        /// </summary>
        [DataField(required: true)]
        public float Wattage;

        /// <summary>
        /// Range of the jammer.
        /// </summary>
        [DataField(required: true)]
        public float Range;

        /// <summary>
        /// The message that is displayed when switched 
        /// to this setting.
        /// </summary>
        [DataField(required: true)]
        public LocId Message = string.Empty;

        /// <summary>
        /// Name of the setting.
        /// </summary>
        [DataField(required: true)]
        public LocId Name = string.Empty;
    }

    /// <summary>
    /// List of all the settings for the radio jammer.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public List<RadioJamSetting> Settings = new();

    /// <summary>
    /// Index of the currently selected setting.
    /// </summary>
    [DataField]
    public int SelectedPowerLevel = 1;
}

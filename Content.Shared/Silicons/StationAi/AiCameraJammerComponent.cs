using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// When activated (ActiveAiCameraJammerComponent) prevents AI cameras from providing vision in range
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class AiCameraJammerComponent : Component
{
    [DataDefinition]
    public partial struct AiCameraJamSetting
    {
        /// <summary>
        /// Power usage per second when enabled.
        /// </summary>
        [DataField(required: true)]
        public float Wattage;

        /// <summary>
        /// Range of the jammer in tiles.
        /// </summary>
        [DataField(required: true)]
        public float Range;

        /// <summary>
        /// The message that is displayed when switched to this setting.
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
    /// List of all the settings for the AI camera jammer.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public AiCameraJamSetting[] Settings = default!;

    /// <summary>
    /// Index of the currently selected setting.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int SelectedPowerLevel = 1;
}

[Serializable, NetSerializable]
public enum AiCameraJammerChargeLevel : byte
{
    Low,
    Medium,
    High
}

[Serializable, NetSerializable]
public enum AiCameraJammerLayers : byte
{
    LED
}

[Serializable, NetSerializable]
public enum AiCameraJammerVisuals : byte
{
    ChargeLevel,
    LEDOn
}

/// <summary>
/// Raised when the jammer's power level is changed
/// </summary>
[ByRefEvent]
public readonly record struct AiCameraJammerPowerLevelChangedEvent(int OldLevel, int NewLevel);

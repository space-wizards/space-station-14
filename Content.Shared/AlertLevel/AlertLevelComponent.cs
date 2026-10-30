using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.AlertLevel;

/// <summary>
/// Alert level component. This is the component given to a station to
/// signify its alert level state.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AlertLevelSystem))]
public sealed partial class AlertLevelComponent : Component
{
    /// <summary>
    /// The available alert levels for this station.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<AlertLevelPrototype>> AvailableAlertLevels = new();

    /// <summary>
    /// The default alert level for this station.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertLevelPrototype> DefaultAlertLevel = "Green";

    /// <summary>
    /// The current level on the station.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertLevelPrototype> CurrentAlertLevel = "Green";

    /// <summary>
    /// Is changing the alert level currently locked?
    /// For example when the nuke is active the level is locked to Delta.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsLevelLocked = false;

    /// <summary>
    /// The time stamp until which changing the alert level is unavailable.
    /// Used to prevent spamming alerts in chat.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? DelayedUntil;
}

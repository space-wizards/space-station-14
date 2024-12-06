using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.AlertLevel;

/// <summary>
/// Alert level component. This is the component given to a station to
/// signify its alert level state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlertLevelComponent : Component
{
    /// <summary>
    /// The current set of alert levels on the station.
    /// </summary>
    [ViewVariables]
    public AlertLevelPrototype? AlertLevels;

    // Once stations are a prototype, this should be used.
    [DataField]
    public AlertLevelPrototype AlertLevelPrototype = default!;

    /// <summary>
    /// The current level on the station.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public string CurrentLevel = string.Empty;

    /// <summary>
    /// Is current station level can be changed by crew.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IsLevelLocked = false;

    [ViewVariables] public float CurrentDelay = 0;
    [ViewVariables] public bool ActiveDelay;

    /// <summary>
    /// Whether this should alert level should toggle emergency access on the entire station
    /// </summary>
    [DataField]
    public bool EnableEmergencyAccess = false;

    /// <summary>
    /// After how long emergency access should trigger for all doors.
    /// </summary>
    [DataField]
    public TimeSpan EmergencyAccessTimer = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Used to hold the state of emergency access on the door prior to a station-destroying event.  This allows us to return to the saved state
    /// after the station-destroying threat is eliminated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreDeltaAlertEmergencyAccessState = false;

    /// <summary>
    ///     Time before delta alert emergency access is reverted, in seconds, after a station-destroying threat is averted.
    /// </summary>
    [DataField]
    public int PostDeltaAlertEmergencyAccessTimer = 10;

    /// <summary>
    ///     Time remaining until the door reverts its emergency access settings after a station-destroying threat is averted.
    /// </summary>
    [DataField]
    public float PostDeltaAlertRemainingEmergencyAccessTimer;

    /// <summary>
    /// Tells us if the a station-destroying threat was recently averted on this airlock's grid.
    /// </summary>
    [ViewVariables]
    public bool DeltaAlertRecentlyEnded;

    /// <summary>
    /// Tells us if the a station-destroying threat is currently ongoing.
    /// </summary>
    [ViewVariables]
    public bool DeltaAlertOngoing;

    /// <summary>
    /// Determines how long to wait during a delta-level event before triggering emergency access.
    /// </summary>
    [ViewVariables]
    public int DeltaAlertEmergencyAccessDelayTime = 180;

    /// <summary>
    /// Timer that keeps track of how long until the door enters emergency access.
    /// </summary>
    [ViewVariables]
    public float DeltaAlertRemainingEmergencyAccessTimer;
    /// <summary>
    /// Determines if the door is currently under delta-level emergency access rules.
    /// </summary>
    [ViewVariables]
    public bool DeltaEmergencyAccessEnabled;

    /// <summary>
    /// If the level can be selected on the station.
    /// </summary>
    [ViewVariables]
    public bool IsSelectable
    {
        get
        {
            if (AlertLevels == null
                || !AlertLevels.Levels.TryGetValue(CurrentLevel, out var level))
            {
                return false;
            }

            return level.Selectable && !level.DisableSelection && !IsLevelLocked;
        }
    }
}

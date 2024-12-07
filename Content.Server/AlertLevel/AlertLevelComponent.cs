using Robust.Shared.GameStates;

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
    [ViewVariables(VVAccess.ReadWrite)]
    public string CurrentLevel = string.Empty;

    /// <summary>
    /// Is current station level can be changed by crew.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsLevelLocked = false;

    [ViewVariables]
    public float CurrentDelay = 0;

    [ViewVariables]
    public bool ActiveDelay;

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
    /// Time before delta alert emergency access is reverted, in seconds, after a station-destroying threat is averted.
    /// </summary>
    [DataField]
    public int PostDeltaAlertEmergencyAccessTimer = 10;

    /// <summary>
    /// Time remaining until the door reverts its emergency access settings after a station-destroying threat is averted.
    /// </summary>
    [ViewVariables]
    public float PostAlertRemainingEmergencyAccessTimer;

    /// <summary>
    /// Tells us if the station-destroying threat was recently averted on this grid.
    /// </summary>
    [ViewVariables]
    public bool DeltaAlertRecentlyEnded;

    /// <summary>
    /// Tells us if the station-destroying threat is currently ongoing.
    /// </summary>
    [ViewVariables]
    public bool EmergencyAlertOngoing;

    /// <summary>
    /// Timer that keeps track of how long until the door enters emergency access.
    /// </summary>
    [ViewVariables]
    public float DeltaAlertRemainingEmergencyAccessTimer;

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

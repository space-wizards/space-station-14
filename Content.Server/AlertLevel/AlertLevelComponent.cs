namespace Content.Server.AlertLevel;

/// <summary>
/// Alert level component. This is the component given to a station to
/// signify its alert level state.
/// </summary>
[RegisterComponent]
public sealed class AlertLevelComponent : Component
{
    /// <summary>
    /// The current set of alert levels on the station.
    /// </summary>
    [ViewVariables]
    public AlertLevelPrototype? AlertLevels;

    // Once stations are a prototype, this should be used.
    [DataField("alertLevelPrototype")]
    public string AlertLevelPrototype = default!;

    /// <summary>
    /// The current level on the station.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public string CurrentLevel = string.Empty;

    /// <summary>
    /// Is current station level can be changed by crew.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IsLevelLocked = false;

    [ViewVariables] public const float Delay = 30;

    [ViewVariables] public float CurrentDelay = 0;
    [ViewVariables] public bool ActiveDelay;

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

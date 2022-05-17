namespace Content.Server.AlertLevel;

// Alert level component. This is the component given to a station.
[RegisterComponent]
public sealed class AlertLevelComponent : Component
{
    [ViewVariables]
    public AlertLevelPrototype? AlertLevels;

    // Once stations are a prototype, this should be used.
    [DataField("alertLevelPrototype")]
    public string AlertLevelPrototype = default!;

    [ViewVariables] public string CurrentLevel = default!;

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

            return level.Selectable && !level.DisableSelection;
        }
    }
}

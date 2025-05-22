using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.AlertLevel;

/// <summary>
/// Alert level component. This is the component given to a station to
/// signify its alert level state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class AlertLevelComponent : Component
{
    /// <summary>
    /// The current set of alert levels on the station.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AlertLevelPrototype? AlertLevels;

    // Once stations are a prototype, this should be used.
    [DataField]
    [AutoNetworkedField]
    public ProtoId<AlertLevelPrototype> AlertLevelPrototype;

    /// <summary>
    /// The current level on the station.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public string CurrentLevel = string.Empty;

    /// <summary>
    /// If the current station level can be changed by crew.
    /// </summary>
    [DataField]
    public bool IsLevelLocked = false;

    [ViewVariables] public float CurrentDelay = 0;
    [ViewVariables] public bool ActiveDelay;

}

using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Changes the alert level of the station when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlertLevelChangeOnTriggerComponent : BaseXOnTriggerComponent
{
    ///<summary>
    /// The alert level to change to when triggered.
    ///</summary>
    [DataField, AutoNetworkedField]
    public string Level = "blue";

    /// <summary>
    /// Whether to play the sound when the alert level changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PlaySound = true;

    /// <summary>
    /// Whether to say the announcement when the alert level changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Announce = true;

    /// <summary>
    /// Force the alert change. This applies if the alert level is not selectable or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Force = false;
}

using Content.Server.AlertLevel.Systems;

namespace Content.Server.AlertLevel;
/// <summary>
/// This component is for changing the alert level of the station when triggered.
/// </summary>
[RegisterComponent, Access(typeof(AlertLevelChangeOnTriggerSystem))]
public sealed partial class AlertLevelChangeOnTriggerComponent : Component
{
    ///<summary>
    ///The alert level to change to when triggered.
    ///</summary>
    [DataField]
    public string Level = "blue";

    /// <summary>
    ///Whether to play the sound when the alert level changes.
    /// </summary>
    [DataField]
    public bool PlaySound = true;

    /// <summary>
    ///Whether to say the announcement when the alert level changes.
    /// </summary>
    [DataField]
    public bool Announce = true;

    /// <summary>
    ///Force the alert change. This applies if the alert level is not selectable or not.
    /// </summary>
    [DataField]
    public bool Force = false;
}

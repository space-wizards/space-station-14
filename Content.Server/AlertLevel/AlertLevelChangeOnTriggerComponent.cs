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
}

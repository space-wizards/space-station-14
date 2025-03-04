using Content.Server.AlertLevel.Systems;

namespace Content.Server.AlertLevel
{
    [RegisterComponent, Access(typeof(AlertLevelChangeOnTriggerSystem))]
    public sealed partial class AlertLevelChangeOnTriggerComponent : Component
    {
        ///<summary>
        ///The alert level to change to when the entity is spawned.
        ///</summary>
        [DataField]
        public string Level = "blue";
    }
}

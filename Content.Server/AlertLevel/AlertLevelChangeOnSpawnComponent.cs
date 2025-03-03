using Content.Server.AlertLevel.Systems;

namespace Content.Server.AlertLevel
{
    [RegisterComponent, Access(typeof(AlertLevelChangeOnSpawnSystem))]
    public sealed partial class AlertLevelChangeOnSpawnComponent : Component
    {
        ///<summary>
        ///The alert level to change to when the entity is spawned.
        ///</summary>
        [DataField(required: true)]
        public string? Level = "Blue";
    }
}

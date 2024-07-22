using Robust.Shared.Prototypes;

namespace Content.Server.Bible.Components
{
    /// <summary>
    /// This lets you summon a mob or item with an alternative verb on the item
    /// </summary>
    [RegisterComponent]
    public sealed partial class SummonableComponent : Component
    {
        /// <summary>
        /// Used for a special item only the Chaplain can summon. Usually a mob, but supports regular items too.
        /// </summary>
        [DataField("specialItem")]
        public EntProtoId? SpecialItemPrototype = null;
        public bool AlreadySummoned = false;

        [DataField("requiresBibleUser")]
        public bool RequiresBibleUser = true;

        /// <summary>
        /// The specific creature this summoned, if the SpecialItemPrototype has a mobstate.
        /// </summary>
        [ViewVariables]
        public EntityUid? Summon = null;

        [DataField]
        public EntProtoId SummonAction = "ActionBibleSummon";

        [DataField("summonActionEntity")]
        public EntityUid? SummonActionEntity;

        /// Used for respawning
        [DataField("accumulator")]
        public float Accumulator = 0f;
        [DataField("respawnTime")]
        public float RespawnTime = 180f;
    }
}

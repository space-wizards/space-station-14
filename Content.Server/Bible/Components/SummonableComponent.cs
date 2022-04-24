using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Server.Bible.Components
{
    /// <summary>
    /// This lets you summon a mob or item with an alternative verb on the item
    /// </summary>
    [RegisterComponent]
    public sealed class SummonableComponent : Component
    {
        /// <summary>
        /// Used for a special item only the Chaplain can summon. Usually a mob, but supports regular items too.
        /// </summary>
        [DataField("specialItem", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? SpecialItemPrototype = null;
        public bool AlreadySummoned = false;

        [DataField("requriesBibleUser")]
        public bool RequiresBibleUser = true;

        /// <summary>
        /// The specific creature this summoned, if the SpecialItemPrototype has a mobstate.
        /// </summary>
        [ViewVariables]
        public EntityUid? Summon = null;

        [DataField("summonAction")]
        public InstantAction SummonAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Clothing/Head/Hats/witch.rsi/icon.png")),
            Name = "bible-summon-verb",
            Description = "bible-summon-verb-desc",
            Event = new SummonActionEvent(),
        };

        /// Used for respawning
        [ViewVariables]
        [DataField("accumulator")]
        public float Accumulator = 0f;
        [ViewVariables]
        [DataField("respawnTime")]
        public float RespawnTime = 180f;
    }
}

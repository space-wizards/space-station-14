using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class PickaxeComponent : Component
    {
        [DataField("sound")]
        public SoundSpecifier MiningSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Mining/pickaxe.ogg");

        [DataField("timeMultiplier")]
        public float MiningTimeMultiplier { get; set; } = 1f;

        /// <summary>
        ///     What damage should be given to objects when
        ///     mined using a pickaxe?
        /// </summary>
        [DataField("damage", required: true)]
        public DamageSpecifier Damage { get; set; } = default!;

        /// <summary>
        ///     How many entities can this pickaxe mine at once?
        /// </summary>
        [DataField("maxEntities")]
        public int MaxMiningEntities = 1;

        public HashSet<EntityUid> MiningEntities = new();
    }
}

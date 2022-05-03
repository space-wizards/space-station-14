using System.Threading;
using Content.Shared.Damage;
using Content.Shared.Sound;

namespace Content.Server.Mining.Components
{
    /// <summary>
    ///     When interacting with an <see cref="MineableComponent"/> allows it to spawn entities.
    /// </summary>
    [RegisterComponent]
    public sealed class PickaxeComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier MiningSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Mining/pickaxe.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
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
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxEntities")]
        public int MaxMiningEntities = 1;

        [ViewVariables]
        public readonly Dictionary<EntityUid, CancellationTokenSource> MiningEntities = new();
    }
}

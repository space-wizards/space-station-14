using System.Threading;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Gatherable.Components
{
    /// <summary>
    ///     When interacting with an <see cref="GatherableComponent"/> allows it to spawn entities.
    /// </summary>
    [RegisterComponent]
    public sealed class GatheringToolComponent : Component
    {
        /// <summary>
        ///     Sound that is made once you completed gathering
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier GatheringSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Mining/pickaxe.ogg");

        /// <summary>
        ///     What damage should be given to objects when
        ///     gathered using this tool? (0 for infinite gathering)
        /// </summary>
        [DataField("damage", required: true)]
        public DamageSpecifier Damage { get; set; } = default!;

        /// <summary>
        ///     How many entities can this tool gather from at once?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxEntities")]
        public int MaxGatheringEntities = 1;

        [ViewVariables]
        [DataField("gatheringEntities")]
        public readonly List<EntityUid> GatheringEntities = new();
    }
}

using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using System.Threading;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Dragon
{
    [RegisterComponent]
    public sealed class DragonComponent : Component
    {
        /// <summary>
        /// The chemical ID injected upon devouring
        /// </summary>
        [DataField("devourChemical", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string DevourChem = "Ichor";

        /// <summary>
        /// The amount of ichor injected per devour
        /// </summary>
        [DataField("devourHealRate")]
        public float DevourHealRate = 15f;

        [DataField("devourActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityTargetActionPrototype>))]
        public string DevourActionId = "DragonDevour";

        [DataField("devourAction")]
        public EntityTargetAction? DevourAction;

        [DataField("spawnActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string SpawnActionId = "DragonSpawn";

        [DataField("spawnAction")]
        public InstantAction? SpawnAction;

        /// <summary>
        /// The amount of time it takes to devour something
        /// <remarks>
        /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
        /// </remarks>
        /// </summary>
        [DataField("devourTime")]
        public float DevourTimer = 15f;

        [DataField("spawns")]
        public EntitySpawnEntry Spawns = new();

        [DataField("deathSound")]
        public SoundSpecifier? DeathSound = new SoundPathSpecifier("/Audio/Animals/sound_creatures_space_dragon_roar.ogg");

        [DataField("devourSound")]
        public SoundSpecifier? DevourSound = new SoundPathSpecifier("/Audio/Effects/sound_magic_demon_consume.ogg");

        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// Where the entities go when dragon devours them, empties when the dragon is dead.
        /// </summary>
        public Container DragonStomach = default!;
    }

    public sealed class DragonDevourAction : EntityTargetAction
    {
        public EntityUid Target;
    }

    public sealed class DragonSpawnAction : InstantAction {}
}

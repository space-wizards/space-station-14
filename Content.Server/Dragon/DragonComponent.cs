using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using System.Threading;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Robust.Shared.Audio;
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
        public float DevourTime = 15f;

        [DataField("spawnCount")] public int SpawnsLeft = 2;

        [DataField("maxSpawnCount")] public int MaxSpawns = 2;

        [DataField("spawnProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? SpawnPrototype = "MobCarpDragon";

        [ViewVariables(VVAccess.ReadWrite), DataField("soundDeath")]
        public SoundSpecifier? SoundDeath = new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg");

        [ViewVariables(VVAccess.ReadWrite), DataField("soundDevour")]
        public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
        {
            Params = AudioParams.Default.WithVolume(-3f),
        };

        [ViewVariables(VVAccess.ReadWrite), DataField("soundStructureDevour")]
        public SoundSpecifier? SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
        {
            Params = AudioParams.Default.WithVolume(-3f),
        };

        [ViewVariables(VVAccess.ReadWrite), DataField("soundRoar")]
        public SoundSpecifier? SoundRoar =
            new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg")
            {
                Params = AudioParams.Default.WithVolume(-3f),
            };

        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// Where the entities go when dragon devours them, empties when the dragon is butchered.
        /// </summary>
        public Container DragonStomach = default!;
    }

    public sealed class DragonDevourActionEvent : EntityTargetActionEvent {}

    public sealed class DragonSpawnActionEvent : InstantActionEvent {}
}

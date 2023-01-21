using System.Threading;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
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
        [ViewVariables(VVAccess.ReadWrite), DataField("devourHealRate")]
        public float DevourHealRate = 15f;

        [DataField("devourActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityTargetActionPrototype>))]
        public string DevourActionId = "DragonDevour";

        [DataField("devourAction")]
        public EntityTargetAction? DevourAction;

        /// <summary>
        /// If we have active rifts.
        /// </summary>
        [DataField("rifts")]
        public List<EntityUid> Rifts = new();

        public bool Weakened => WeakenedAccumulator > 0f;

        /// <summary>
        /// When any rift is destroyed how long is the dragon weakened for
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("weakenedDuration")]
        public float WeakenedDuration = 120f;

        /// <summary>
        /// Has a rift been destroyed and the dragon in a temporary weakened state?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("weakenedAccumulator")]
        public float WeakenedAccumulator = 0f;

        [ViewVariables(VVAccess.ReadWrite), DataField("riftAccumulator")]
        public float RiftAccumulator = 0f;

        /// <summary>
        /// Maximum time the dragon can go without spawning a rift before they die.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("maxAccumulator")] public float RiftMaxAccumulator = 300f;

        /// <summary>
        /// Spawns a rift which can summon more mobs.
        /// </summary>
        [DataField("spawnRiftAction")]
        public InstantAction? SpawnRiftAction;

        [ViewVariables(VVAccess.ReadWrite), DataField("riftPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string RiftPrototype = "CarpRift";

        /// <summary>
        /// The amount of time it takes to devour something
        /// <remarks>
        /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
        /// </remarks>
        /// </summary>
        [DataField("structureDevourTime")]
        public float StructureDevourTime = 10f;

        [DataField("devourTime")]
        public float DevourTime = 3f;

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
                Params = AudioParams.Default.WithVolume(3f),
            };

        public CancellationTokenSource? CancelToken;

        [ViewVariables(VVAccess.ReadWrite), DataField("devourWhitelist")]
        public EntityWhitelist? DevourWhitelist = new()
        {
            Components = new[]
            {
                "Door",
                "MobState",
            },
            Tags = new List<string>
            {
                "Wall",
            },
        };

        /// <summary>
        /// Where the entities go when dragon devours them, empties when the dragon is butchered.
        /// </summary>
        public Container DragonStomach = default!;
    }

    public sealed class DragonDevourActionEvent : EntityTargetActionEvent {}

    public sealed class DragonSpawnRiftActionEvent : InstantActionEvent {}
}

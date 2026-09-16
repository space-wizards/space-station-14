using Content.Shared.Chemistry.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Dragon
{
    // TODO: use timespans for logic
    [RegisterComponent, AutoGenerateEntityRelations(shutdownEvent: false)]
    public sealed partial class DragonComponent : Component
    {
        /// <summary>
        /// If we have active rifts.
        /// </summary>
        [DataField, AutoRelationField]
        public List<EntityRelation> Rifts = new();

        public bool Weakened => WeakenedAccumulator > 0f;

        /// <summary>
        /// When any rift is destroyed how long is the dragon weakened for
        /// </summary>
        [DataField]
        public float WeakenedDuration = 120f;

        /// <summary>
        /// Has a rift been destroyed and the dragon in a temporary weakened state?
        /// </summary>
        [DataField]
        public float WeakenedAccumulator = 0f;

        [DataField]
        public float RiftAccumulator = 0f;

        /// <summary>
        /// Maximum time the dragon can go without spawning a rift before they die.
        /// </summary>
        [DataField]
        public float RiftMaxAccumulator = 300f;

        [DataField]
        public EntProtoId SpawnRiftAction = "ActionSpawnRift";

        /// <summary>
        /// Spawns a rift which can summon more mobs.
        /// </summary>
        [DataField]
        public EntityUid? SpawnRiftActionEntity;

        [DataField]
        public EntProtoId RiftPrototype = "CarpRift";

        [DataField]
        public SoundSpecifier? SoundDeath = new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg");

        [DataField]
        public SoundSpecifier? SoundRoar =
            new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg")
            {
                Params = AudioParams.Default.AddVolume(3f),
            };

        /// <summary>
        /// NPC faction to re-add after being zombified.
        /// Prevents zombie dragon from being attacked by its own carp.
        /// </summary>
        [DataField]
        public ProtoId<NpcFactionPrototype> Faction = "Dragon";

        /// <summary>
        /// The smoke to spawn upon rift timeout death.
        /// </summary>
        [DataField]
        public EntProtoId SmokePrototype = "BloodSmoke";

        /// <summary>
        /// The solution to place into the smoke (mostly just needed for color)
        /// </summary>
        [DataField]
        public Solution SmokeSolution = new ([new("Blood", 1)]);
    }
}

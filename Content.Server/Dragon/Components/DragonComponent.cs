using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Dragon
{
    [RegisterComponent]
    public sealed partial class DragonComponent : Component
    {

        /// <summary>
        /// If we have active rifts.
        /// </summary>
        [DataField]
        public List<EntityUid> Rifts = new();

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
        /// The time after which the dragon will receive a popup that it needs to set the Rift.
        /// </summary>
        [DataField]
        public int RiftPopupAlertAccumulator = 200;

        /// <summary>
        /// The popup alert message shown to the dragon when they need to spawn a rift.
        /// </summary>
        [DataField]
        public LocId RiftPopupAlert = "dragon-rift-alert";

        /// <summary>
        /// Announcement of fully charging rift
        /// </summary>
        [DataField]
        public bool DragonAlerted;

        /// <summary>
        /// Maximum time the dragon can go without spawning a rift before they die.
        /// </summary>
        [DataField]
        public float RiftMaxAccumulator = 300f;

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SpawnRiftAction = "ActionSpawnRift";

        /// <summary>
        /// Spawns a rift which can summon more mobs.
        /// </summary>
        [DataField]
        public EntityUid? SpawnRiftActionEntity;

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string RiftPrototype = "CarpRift";

        [DataField]
        public SoundSpecifier? SoundDeath = new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg");

        [DataField]
        public SoundSpecifier? SoundRoar =
            new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg")
            {
                Params = AudioParams.Default.WithVolume(3f),
            };

        /// <summary>
        /// NPC faction to re-add after being zombified.
        /// Prevents zombie dragon from being attacked by its own carp.
        /// </summary>
        [DataField]
        public ProtoId<NpcFactionPrototype> Faction = "Dragon";
    }
}

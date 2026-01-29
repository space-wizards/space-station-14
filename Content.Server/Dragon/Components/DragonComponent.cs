using Content.Server.Database;
using Content.Shared.EntityEffects;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Text.Json.Serialization;
using static Content.Shared.Fax.AdminFaxEuiMsg;

namespace Content.Server.Dragon
{
    [RegisterComponent]
    public sealed partial class DragonComponent : Component
    {

        /// <summary>
        /// If we have active rifts.
        /// </summary>
        [DataField("rifts")]
        public List<EntityUid> Rifts = new();

        /// <summary>
        /// The number of charged rifts.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), DataField]
        public int ChargedRifts = 0;

        /// <summary>
        /// The maximum number of rifts that a dragon can create.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), DataField]
        public int MaxRifts = 3;

        /// <summary>
        /// The effects that appear on the dragon when the maximum possible number of rifts is fully charged.
        /// </summary>
        [DataField]
        public EntityEffect[] FullPowerEffects = default!;

        /// <summary>
        /// The list of components that will be added if the dragon when the maximum possible number of rifts is fully charged.
        /// </summary>
        [DataField(required: true)]
        public ComponentRegistry FullPowerComponents = new();

        /// <summary>
        /// Text of the alert when calling the evacuation shuttle if the dragon fully charges 3 rifts.
        /// </summary>
        [DataField]
        public LocId RoundEndText = "dragon-round-end";

        public bool Weakened => WeakenedAccumulator > 0f;

        /// <summary>
        /// When any rift is destroyed how long is the dragon weakened for.
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

        [DataField("spawnRiftAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SpawnRiftAction = "ActionSpawnRift";

        /// <summary>
        /// Spawns a rift which can summon more mobs.
        /// </summary>
        [DataField("spawnRiftActionEntity")]
        public EntityUid? SpawnRiftActionEntity;

        [ViewVariables(VVAccess.ReadWrite), DataField("riftPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string RiftPrototype = "CarpRift";

        [ViewVariables(VVAccess.ReadWrite), DataField("soundDeath")]
        public SoundSpecifier? SoundDeath = new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg");

        [ViewVariables(VVAccess.ReadWrite), DataField("soundRoar")]
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

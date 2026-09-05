using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Dragon
{
    // TODO: use timespans for logic
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
        
        /// <summary>
        /// This alert displays how long the dragon has to spawn a rift
        /// </summary>
        [DataField]
        public ProtoId<AlertPrototype> RiftTimerAlert = "DragonRiftTimer";

        /// <summary>
        /// How often the threshold for the alert icon will be checked (blue/orange/red)
        /// </summary>
        [DataField]
        public TimeSpan RiftTimerThresholdCheckInterval = TimeSpan.FromSeconds(3f);

        /// <summary>
        /// The time threshold for changing the alert icon color
        /// </summary>
        [DataField]
        public Dictionary<RiftTimerThreshold, float> RiftTimerThresholds = new()
        {
            { RiftTimerThreshold.Blue, 300f },
            { RiftTimerThreshold.Orange, 150f },
            { RiftTimerThreshold.Red, 60f }
        };
    }

    /// <summary>
    /// The different color thresholds for the rift timer alert
    /// </summary>
    public enum RiftTimerThreshold : byte
    {
        Blue = 1 << 1,
        Orange = 1 << 0,
        Red = 0,
    }
}

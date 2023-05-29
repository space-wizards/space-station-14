using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Flesh
{
    [RegisterComponent]
    [Access(typeof(FleshHeartSystem))]
    public sealed class FleshHeartComponent : Component
    {
        [DataField("transformSound")] public SoundSpecifier TransformSound = new SoundCollectionSpecifier("gib");

        [ViewVariables(VVAccess.ReadWrite), DataField("speciesWhitelist")]
        public List<string> SpeciesWhitelist = new()
        {
            "Human",
            "Reptilian",
            "Dwarf",
        };

        [DataField("alertLevelOnActivate")] public string AlertLevelOnActivate = "red";

        [DataField("alertLevelOnDeactivate")] public string AlertLevelOnDeactivate = "green";

        public IPlayingAudioStream? AmbientAudioStream = default;

        [DataField("bodyToFinalStage"), ViewVariables(VVAccess.ReadWrite)]
        public int BodyToFinalStage = 3; // default 3

        [DataField("timeLiveFinalHeartToWin"), ViewVariables(VVAccess.ReadWrite)]
        public int TimeLiveFinalHeartToWin = 1200; // default 600

        [DataField("spawnObjectsFrequency"), ViewVariables(VVAccess.ReadWrite)]
        public float SpawnObjectsFrequency = 60;

        [DataField("spawnObjectsAmount"), ViewVariables(VVAccess.ReadWrite)]
        public int SpawnObjectsAmount = 6;

        [DataField("spawnObjectsRadius"), ViewVariables(VVAccess.ReadWrite)]
        public float SpawnObjectsRadius = 5;

        [DataField("fleshTileId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)),
         ViewVariables(VVAccess.ReadWrite)]
        public string FleshTileId = "Flesh";

        [DataField("fleshBlockerId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)),
         ViewVariables(VVAccess.ReadWrite)]
        public string FleshBlockerId = "FleshBlocker";

        [DataField("spawns"), ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<string, float> Spawns = new();

        [DataField("spawnMobsFrequency"), ViewVariables(VVAccess.ReadWrite)]
        public float SpawnMobsFrequency = 120;

        [DataField("spawnMobsAmount"), ViewVariables(VVAccess.ReadWrite)]
        public int SpawnMobsAmount = 4;

        [DataField("spawnMobsRadius"), ViewVariables(VVAccess.ReadWrite)]
        public float SpawnMobsRadius = 3;

        [ViewVariables]
        public float Accumulator = 0;

        [ViewVariables]
        public float SpawnMobsAccumulator = 0;

        [ViewVariables]
        public float SpawnObjectsAccumulator = 0;

        [ViewVariables]
        public float FinalStageAccumulator = 0;

        [ViewVariables]
        public FleshHeartSystem.HeartStates State = FleshHeartSystem.HeartStates.Base;

        public readonly HashSet<EntityUid> EdgeMobs = new();

        public Container BodyContainer = default!;

        [DataField("damageMobsIfHeartDestruct", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageMobsIfHeartDestruct = default!;
    }
}

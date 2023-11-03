using Content.Shared.BluespaceHarvester;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterComponent : Component
{
    [DataField("currentLevel"), ViewVariables(VVAccess.ReadWrite)]
    public int CurrentLevel = 0;

    [DataField("targetLevel"), ViewVariables(VVAccess.ReadWrite)]
    public int TargetLevel = 0;

    [DataField("maxLevel")]
    public int MaxLevel = 20;

    [DataField("redspaceTap")]
    public BluespaceHarvesterVisuals RedspaceTap = BluespaceHarvesterVisuals.TapRedspace;

    [DataField("stabelLevel")]
    public int StableLevel = 10;

    [DataField("emaggedStableLevel")]
    public int EmaggedStableLevel = 5;

    [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
    public int Points = 0;

    [DataField("totalPoints"), ViewVariables(VVAccess.ReadWrite)]
    public int TotalPoints = 0;

    [DataField("dangerPoints"), ViewVariables(VVAccess.ReadWrite)]
    public int Danger = 0;

    [DataField("reseted")]
    public bool Enable = false;

    [DataField("spawnRadius")]
    public float SpawnRadius = 5f;

    [DataField("rift", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Rift = "BluespaceHarvesterRift";

    [DataField("riftChance")]
    public float RiftChance = 0.1f;

    [DataField("emaggedRiftChance")]
    public float EmaggedRiftChance = 0.05f;

    /// <summary>
    ///     After this danger value, the generation of dangerous creatures and anomalies will begin.
    /// </summary>
    [DataField("dangerLimit")]
    public int DangerLimit = 175;

    [DataField("categories")]
    public List<BluespaceHarvesterCategoryInfo> Categories = new()
    {
        new BluespaceHarvesterCategoryInfo()
        {
            PrototypeId = "RandomHarvesterBiologicalLoot",
            Cost = 7_500,
            Type = BluespaceHarvesterCategory.Biological,
        },
        new BluespaceHarvesterCategoryInfo()
        {
            PrototypeId = "RandomHarvesterTechnologicalLoot",
            Cost = 10_000,
            Type = BluespaceHarvesterCategory.Technological,
        },
        new BluespaceHarvesterCategoryInfo()
        {
            PrototypeId = "RandomHarvesterIndustrialLoot",
            Cost = 12_500,
            Type = BluespaceHarvesterCategory.Industrial,
        },
        new BluespaceHarvesterCategoryInfo()
        {
            PrototypeId = "RandomHarvesterDestructionLoot",
            Cost = 15_000,
            Type = BluespaceHarvesterCategory.Destruction,
        },
    };

    [DataField("spawnEffect"), ViewVariables(VVAccess.ReadWrite)]
    public string SpawnEffectPrototype = "EffectEmpPulse";

    [DataField("spawnSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}

[Serializable]
public sealed partial class BluespaceHarvesterTap
{
    [DataField("level")]
    public int Level;

    [DataField("visual")]
    public BluespaceHarvesterVisuals Visual;
}

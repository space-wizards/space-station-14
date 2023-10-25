using Content.Shared.BluespaceHarvester;

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
    public int DangerPoints = 0;

    [DataField("reseted")]
    public bool Enable = false;

    [DataField("spawnRadius")]
    public float SpawnRadius = 5f;

    /// <summary>
    ///     After this danger value, the generation of dangerous creatures and anomalies will begin.
    /// </summary>
    [DataField("dangerLimit")]
    public int DangerLimit = 100;

    [DataField("categories")]
    public List<BluespaceHarvesterCategoryInfo> Categories = new()
    {
        new BluespaceHarvesterCategoryInfo()
        {
            PrototypeId = "RandomHarvesterTechnicalLoot",
            Cost = 15_000,
            Type = BluespaceHarvesterCategory.Technical,
        },
    };
}

[Serializable]
public sealed partial class BluespaceHarvesterTap
{
    [DataField("level")]
    public int Level;

    [DataField("visual")]
    public BluespaceHarvesterVisuals Visual;
}

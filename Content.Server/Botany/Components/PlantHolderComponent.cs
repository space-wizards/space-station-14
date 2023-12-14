using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class PlantHolderComponent : Component
{
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadWrite), DataField("updateDelay")]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField("lastProduce")]
    public int LastProduce;

    [ViewVariables(VVAccess.ReadWrite), DataField("missingGas")]
    public int MissingGas;

    [DataField("cycleDelay")]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    [DataField("lastCycle", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("updateSpriteAfterUpdate")]
    public bool UpdateSpriteAfterUpdate;

    [ViewVariables(VVAccess.ReadWrite), DataField("drawWarnings")]
    public bool DrawWarnings = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("waterLevel")]
    public float WaterLevel = 100f;

    [ViewVariables(VVAccess.ReadWrite), DataField("nutritionLevel")]
    public float NutritionLevel = 100f;

    [ViewVariables(VVAccess.ReadWrite), DataField("pestLevel")]
    public float PestLevel;

    [ViewVariables(VVAccess.ReadWrite), DataField("weedLevel")]
    public float WeedLevel;

    [ViewVariables(VVAccess.ReadWrite), DataField("toxins")]
    public float Toxins;

    [ViewVariables(VVAccess.ReadWrite), DataField("age")]
    public int Age;

    [ViewVariables(VVAccess.ReadWrite), DataField("skipAging")]
    public int SkipAging;

    [ViewVariables(VVAccess.ReadWrite), DataField("dead")]
    public bool Dead;

    [ViewVariables(VVAccess.ReadWrite), DataField("harvest")]
    public bool Harvest;

    [ViewVariables(VVAccess.ReadWrite), DataField("sampled")]
    public bool Sampled;

    [ViewVariables(VVAccess.ReadWrite), DataField("yieldMod")]
    public int YieldMod = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("mutationMod")]
    public float MutationMod = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("mutationLevel")]
    public float MutationLevel;

    [ViewVariables(VVAccess.ReadWrite), DataField("health")]
    public float Health;

    [ViewVariables(VVAccess.ReadWrite), DataField("weedCoefficient")]
    public float WeedCoefficient = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("seed")]
    public SeedData? Seed;

    [ViewVariables(VVAccess.ReadWrite), DataField("improperHeat")]
    public bool ImproperHeat;

    [ViewVariables(VVAccess.ReadWrite), DataField("improperPressure")]
    public bool ImproperPressure;

    [ViewVariables(VVAccess.ReadWrite), DataField("improperLight")]
    public bool ImproperLight;

    [ViewVariables(VVAccess.ReadWrite), DataField("forceUpdate")]
    public bool ForceUpdate;

    [ViewVariables(VVAccess.ReadWrite), DataField("solution")]
    public string SoilSolutionName = "soil";
}

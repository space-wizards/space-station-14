using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class PlantHolderComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField()]
    public int LastProduce;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public int MissingGas;

    [DataField()]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool UpdateSpriteAfterUpdate;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool DrawWarnings = false;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float WaterLevel = 100f;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float NutritionLevel = 100f;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float PestLevel;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float WeedLevel;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float Toxins;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public int Age;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public int SkipAging;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool Dead;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool Harvest;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool Sampled;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float YieldMod = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float MutationMod = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float MutationLevel;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float Health;

    ///<summary>
    /// The bonus to potency from fertiliser applied to the curent crop. This is added with the seed's own Potency to determine things like crop reagent contents.
    ///</summary>
    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float PotencyBonus;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public float WeedCoefficient = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public SeedData? Seed;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool ImproperHeat;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool ImproperPressure;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool ImproperLight;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public bool ForceUpdate;

    [ViewVariables(VVAccess.ReadWrite), DataField()]
    public string SoilSolutionName = "soil";

    [DataField]
    public Entity<SolutionComponent>? SoilSolution = null;
}

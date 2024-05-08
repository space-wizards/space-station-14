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

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float NutritionLevel = 100f;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float PestLevel;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float WeedLevel;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float Toxins;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public int Age;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public int SkipAging;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool Dead;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool Harvest;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool Sampled;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float YieldMod = 1;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float MutationMod = 1f;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float MutationLevel;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float Health;

    ///<summary>
    /// The bonus to potency from fertiliser applied to the curent crop. This is added with the seed's own Potency to determine things like crop reagent contents.
    ///</summary>
    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float PotencyBonus;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public float WeedCoefficient = 1f;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public SeedData? Seed;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool ImproperHeat;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool ImproperPressure;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool ImproperLight;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public bool ForceUpdate;

    [ViewVariables(VVAccess.ReadWrite), Datafield()]
    public string SoilSolutionName = "soil";

    [DataField]
    public Entity<SolutionComponent>? SoilSolution = null;
}

using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class PlantHolderComponent : Component
{
    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Time between plant reagent consumption updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public int LastProduce;

    [DataField]
    public int MissingGas;

    /// <summary>
    /// Time between plant growth updates.
    /// </summary>
    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    /// <summary>
    /// Game time when the plant last did a growth update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    /// <summary>
    /// Sound played when any reagent is transferred into the plant holder.
    /// </summary>
    [DataField]
    public SoundSpecifier? WateringSound;

    [DataField]
    public bool UpdateSpriteAfterUpdate;

    /// <summary>
    /// Set to true if the plant holder displays plant warnings (e.g. water low) in the sprite and
    /// examine text. Used to differentiate hydroponic trays from simple soil plots.
    /// </summary>
    [DataField]
    public bool DrawWarnings = false;

    [DataField]
    public float WaterLevel = 100f;

    [DataField]
    public float NutritionLevel = 100f;

    [DataField]
    public float PestLevel;

    [DataField]
    public float WeedLevel;

    [DataField]
    public float Toxins;

    [DataField]
    public int Age;

    [DataField]
    public int SkipAging;

    [DataField]
    public bool Dead;

    [DataField]
    public bool Harvest;

    /// <summary>
    /// Set to true if this plant has been clipped by seed clippers. Used to prevent a single plant
    /// from repeatedly being clipped.
    /// </summary>
    [DataField]
    public bool Sampled;

    /// <summary>
    /// Multiplier for the number of entities produced at harvest.
    /// </summary>
    [DataField]
    public int YieldMod = 1;

    [DataField]
    public float MutationMod = 1f;

    [DataField]
    public float MutationLevel;

    [DataField]
    public float Health;

    [DataField]
    public float WeedCoefficient = 1f;

    [DataField]
    public SeedData? Seed;

    /// <summary>
    /// True if the plant is losing health due to too high/low temperature.
    /// </summary>
    [DataField]
    public bool ImproperHeat;

    /// <summary>
    /// True if the plant is losing health due to too high/low pressure.
    /// </summary>
    [DataField]
    public bool ImproperPressure;

    /// <summary>
    /// Not currently used.
    /// </summary>
    [DataField]
    public bool ImproperLight;

    /// <summary>
    /// Set to true to force a plant update (visuals, component, etc.) regardless of the current
    /// update cycle time. Typically used when some interaction affects this plant.
    /// </summary>
    [DataField]
    public bool ForceUpdate;

    [DataField]
    public string SoilSolutionName = "soil";

    [ViewVariables]
    public Entity<SolutionComponent>? SoilSolution = null;
}

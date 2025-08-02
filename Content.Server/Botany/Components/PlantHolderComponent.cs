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

    /// <summary>
    /// Age when the plant last produced harvestable items.
    /// </summary>
    [DataField]
    public int LastProduce;

    /// <summary>
    /// Number of missing gases required for plant growth.
    /// </summary>
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

    /// <summary>
    /// Whether to update the sprite after the next update cycle.
    /// </summary>
    [DataField]
    public bool UpdateSpriteAfterUpdate;

    /// <summary>
    /// Set to true if the plant holder displays plant warnings (e.g. water low) in the sprite and
    /// examine text. Used to differentiate hydroponic trays from simple soil plots.
    /// </summary>
    [DataField]
    public bool DrawWarnings = false;

    /// <summary>
    /// Current water level in the plant holder (0-100).
    /// </summary>
    [DataField]
    public float WaterLevel = 100f;

    /// <summary>
    /// Current nutrient level in the plant holder (0-100).
    /// </summary>
    [DataField]
    public float NutritionLevel = 100f;

    /// <summary>
    /// Current pest level in the plant holder (0-10).
    /// </summary>
    [DataField]
    public float PestLevel;

    /// <summary>
    /// Current weed level in the plant holder (0-10).
    /// </summary>
    [DataField]
    public float WeedLevel;

    /// <summary>
    /// Current toxin level in the plant holder (0-100).
    /// </summary>
    [DataField]
    public float Toxins;

    /// <summary>
    /// Current age of the plant in growth cycles.
    /// </summary>
    [DataField]
    public int Age;

    /// <summary>
    /// Number of growth cycles to skip due to poor conditions.
    /// </summary>
    [DataField]
    public int SkipAging;

    /// <summary>
    /// Whether the plant is dead.
    /// </summary>
    [DataField]
    public bool Dead;

    /// <summary>
    /// Whether the plant is ready for harvest.
    /// </summary>
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

    /// <summary>
    /// Multiplier for mutation chance and severity.
    /// </summary>
    [DataField]
    public float MutationMod = 1f;

    /// <summary>
    /// Current mutation level (0-100).
    /// </summary>
    [DataField]
    public float MutationLevel;

    /// <summary>
    /// Current health of the plant (0 to seed endurance).
    /// </summary>
    [DataField]
    public float Health;

    /// <summary>
    /// Multiplier for weed growth rate.
    /// </summary>
    [DataField]
    public float WeedCoefficient = 1f;

    /// <summary>
    /// Seed data for the currently planted seed.
    /// </summary>
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

    /// <summary>
    /// Name of the solution container that holds the soil/nutrient solution.
    /// </summary>
    [DataField]
    public string SoilSolutionName = "soil";

    /// <summary>
    /// Reference to the soil solution container.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? SoilSolution = null;
}

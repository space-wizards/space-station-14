using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for hydroponics trays plots that hold resources and link to a plant entity.
/// </summary>
[RegisterComponent]
public sealed partial class PlantTrayComponent : Component
{
    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

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
    /// Time between plant reagent consumption updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

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
    /// Set to true to force an update cycle regardless of timing.
    /// </summary>
    [DataField]
    public bool ForceUpdate;

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
    /// Multiplier for weed growth rate.
    /// </summary>
    [DataField]
    public float WeedCoefficient = 1f;

    /// <summary>
    /// Current toxin level in the plant holder (0-100).
    /// </summary>
    [DataField]
    public float Toxins;

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
    /// Name of the solution container that holds the soil/nutrient solution.
    /// </summary>
    [DataField]
    public string SoilSolutionName = "soil";

    /// <summary>
    /// Reference to the soil solution container.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? SoilSolution = null;

    /// <summary>
    /// Currently planted plant entity (parented to this tray).
    /// </summary>
    [ViewVariables]
    public EntityUid? PlantEntity;

    /// <summary>
    /// Multiplier for plant growth speed in the tray.
    /// </summary>
    [DataField]
    public float TraySpeedMultiplier = 1f;

    /// <summary>
    /// Multiplier for resource consumption (water, nutrients) in the tray.
    /// </summary>
    [DataField]
    public float TrayConsumptionMultiplier = 2f;
}

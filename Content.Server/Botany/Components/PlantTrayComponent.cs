using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for hydroponics trays plots that hold resources and link to a plant entity.
/// </summary>
[RegisterComponent]
[Access(typeof(PlantTraySystem))]
public sealed partial class PlantTrayComponent : Component
{
    /// <summary>
    /// Current water level in the plant (0-100).
    /// </summary>
    [DataField]
    public float WaterLevel = 100f;

    [DataField]
    public float MaxWaterLevel = 100f;

    /// <summary>
    /// Current nutrient level in the plant (0-100).
    /// </summary>
    [DataField]
    public float NutritionLevel = 100f;

    [DataField]
    public float MaxNutritionLevel = 100f;

    /// <summary>
    /// Set to true if the plant holder displays plant warnings (e.g. water low) in the sprite and
    /// examine text. Used to differentiate hydroponic trays from simple soil plots.
    /// </summary>
    [DataField]
    public bool DrawWarnings;

    /// <summary>
    /// Sound played when any reagent is transferred into the tray.
    /// </summary>
    [DataField]
    public SoundSpecifier? WateringSound;

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
    /// Current weed level in the plant.
    /// </summary>
    [DataField]
    public float WeedLevel;

    [DataField]
    public float MaxWeedLevel = 10f;

    /// <summary>
    /// Multiplier for weed growth rate.
    /// </summary>
    [DataField]
    public float WeedCoefficient = 1f;

    /// <summary>
    /// Currently planted plant entity (parented to this tray).
    /// </summary>
    [ViewVariables]
    public EntityUid? PlantEntity;

    /// <summary>
    /// Multiplier for resource consumption (water, nutrients) in the tray.
    /// </summary>
    [DataField]
    public float TrayConsumptionMultiplier = 2f;

    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The basic tick for updating the tray, between which most of the tray logic processing takes place.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Chance per tick for weeds to grow around this tray.
    /// </summary>
    [DataField]
    public float WeedGrowthChance = 0.05f;

    /// <summary>
    /// Amount of weed growth per successful weed tray tick.
    /// </summary>
    [DataField]
    public float WeedGrowthAmount = 0.1f;
}

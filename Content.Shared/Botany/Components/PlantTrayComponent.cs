using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for hydroponics trays plots that hold resources and link to a plant entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(PlantTraySystem))]
public sealed partial class PlantTrayComponent : Component
{
    /// <summary>
    /// Current water level in the plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaterLevel = 100f;

    [DataField, AutoNetworkedField]
    public float MaxWaterLevel = 100f;

    /// <summary>
    /// Current nutrient level in the plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NutritionLevel = 100f;

    [DataField, AutoNetworkedField]
    public float MaxNutritionLevel = 100f;

    /// <summary>
    /// Current weed level in the plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedLevel;

    [DataField, AutoNetworkedField]
    public float MaxWeedLevel = 10f;

    /// <summary>
    /// Chance per tick for weeds to grow around this tray.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedGrowthChance = 0.05f;

    /// <summary>
    /// Amount of weed growth per successful weed tray tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedGrowthAmount = 0.1f;

    /// <summary>
    /// Multiplier for weed growth rate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WeedCoefficient = 1f;

    /// <summary>
    /// Multiplier for resource consumption (water, nutrients) in the tray.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TrayConsumptionMultiplier = 2f;

    /// <summary>
    /// Currently planted plant entity (parented to this tray).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? PlantEntity;

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
    [ViewVariables]
    public string SoilSolutionName = "soil";

    /// <summary>
    /// Reference to the soil solution container.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? SoilSolution = null;

    /// <summary>
    /// Game time for the next plant reagent update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The basic tick for updating the tray, between which most of the tray logic processing takes place.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);
}

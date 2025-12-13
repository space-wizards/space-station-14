using System.Linq;
using System.Reflection;

namespace Content.Server.Botany.Components;

/// <summary>
/// TODO: Delete after plants transition to entities.
/// This is an intentionally evil approach kept only to simplify the
/// upcoming refactor: plants will become standalone entities that own these components.
/// Once that happens, this holder is no longer needed.
/// </summary>
[DataDefinition]
public sealed partial class GrowthComponentsHolder
{
    public static readonly PropertyInfo[] ComponentGetters = typeof(GrowthComponentsHolder).GetProperties();
    public static readonly Type[] GrowthComponentTypes = ComponentGetters.Select(x => x.PropertyType).ToArray();

    /// <summary>
    /// Extra-traits.
    /// </summary>
    [DataField]
    public PlantTraitsComponent? PlantTraits { get; set; }

    /// <summary>
    /// Plant characteristics.
    /// </summary>
    [DataField]
    public PlantComponent? Plant { get; set; }

    /// <summary>
    /// Basic properties for plant growth.
    /// </summary>
    [DataField]
    public BasicGrowthComponent? BasicGrowth { get; set; }

    /// <summary>
    /// What defence plant have against toxins?
    /// </summary>
    [DataField]
    public PlantToxinsComponent? Toxins { get; set; }

    /// <summary>
    /// Harvesting process-related data.
    /// </summary>
    [DataField]
    public PlantHarvestComponent? Harvest { get; set; }

    /// <summary>
    /// Atmos-related environment requirements for plant growth.
    /// </summary>
    [DataField]
    public AtmosphericGrowthComponent? AtmosphericGrowth { get; set; }

    /// <summary>
    /// What gases plant consume/exude upon growth.
    /// </summary>
    [DataField]
    public ConsumeExudeGasGrowthComponent? ConsumeExudeGasGrowth { get; set; }

    /// <summary>
    /// Weeds and pests related data for plant.
    /// </summary>
    [DataField]
    public WeedPestGrowthComponent? WeedPestGrowth { get; set; }

    /// <summary>
    /// Damage tolerance of plant.
    /// </summary>
    [DataField]
    public UnviableGrowthComponent? UnviableGrowth { get; set; }

    /// <summary>
    /// Populates any null properties with default component instances so that
    /// systems can always apply a full set. Existing (YAML-provided) values are kept.
    /// </summary>
    public void EnsureGrowthComponents()
    {
        foreach (var prop in ComponentGetters)
        {
            if (prop.GetValue(this) == null)
            {
                var instance = Activator.CreateInstance(prop.PropertyType); // this is really cursed and should not be used in master, also this should be blocked by sandboxing.
                prop.SetValue(this, instance);
            }
        }
    }
}

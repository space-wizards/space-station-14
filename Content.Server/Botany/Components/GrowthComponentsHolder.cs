namespace Content.Server.Botany.Components;

/// <summary>
/// Holder for plant growth settings serialized from YAML as a map under "growthComponents".
/// Each property corresponds to a concrete growth component type.
/// </summary>
[DataDefinition]
public sealed partial class GrowthComponentsHolder
{
    [DataField("plantTraits")]
    public PlantTraitsComponent? PlantTraits { get; set; }

    [DataField("basicGrowth")]
    public BasicGrowthComponent? BasicGrowth { get; set; }

    [DataField("toxins")]
    public ToxinsComponent? Toxins { get; set; }

    [DataField("harvest")]
    public HarvestComponent? Harvest { get; set; }

    [DataField("atmosphericGrowth")]
    public AtmosphericGrowthComponent? AtmosphericGrowth { get; set; }

    [DataField("consumeExudeGasGrowth")]
    public ConsumeExudeGasGrowthComponent? ConsumeExudeGasGrowth { get; set; }

    [DataField("weedPestGrowth")]
    public WeedPestGrowthComponent? WeedPestGrowth { get; set; }

    [DataField("unviableGrowth")]
    public UnviableGrowthComponent? UnviableGrowth { get; set; }

    /// <summary>
    /// Populates any null properties with default component instances so that
    /// systems can always apply a full set. Existing (YAML-provided) values are kept.
    /// </summary>
    public void EnsureGrowthComponents()
    {
        foreach (var prop in typeof(GrowthComponentsHolder).GetProperties())
        {
            if (prop.GetValue(this) == null)
            {
                var instance = System.Activator.CreateInstance(prop.PropertyType);
                prop.SetValue(this, instance);
            }
        }
    }
}

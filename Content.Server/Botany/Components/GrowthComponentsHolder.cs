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
    [DataField]
    public PlantTraitsComponent? PlantTraits { get; set; }

    [DataField]
    public BasicGrowthComponent? BasicGrowth { get; set; }

    [DataField]
    public PlantToxinsComponent? Toxins { get; set; }

    [DataField]
    public PlantHarvestComponent? Harvest { get; set; }

    [DataField]
    public AtmosphericGrowthComponent? AtmosphericGrowth { get; set; }

    [DataField]
    public ConsumeExudeGasGrowthComponent? ConsumeExudeGasGrowth { get; set; }

    [DataField]
    public WeedPestGrowthComponent? WeedPestGrowth { get; set; }

    [DataField]
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
                var instance = Activator.CreateInstance(prop.PropertyType);
                prop.SetValue(this, instance);
            }
        }
    }
}

namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class AutoHarvestGrowthComponent : PlantGrowthComponent
{
    /// <summary>
    /// Chance per tick for the plant to automatically harvest itself.
    /// </summary>
    [DataField("harvestChance")]
    public float HarvestChance = 0.1f;
}

namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class AutoHarvestGrowthComponent : PlantGrowthComponent
{
    [DataField("harvestChance")]
    public float HarvestChance = 0.1f;
}

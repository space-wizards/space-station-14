namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class NutrientGrowthComponent : PlantGrowthComponent
{
    [DataField]
    public float NutrientConsumption = 0.75f;
}

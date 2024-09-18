namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class WaterGrowthComponent : PlantGrowthComponent
{
    [DataField]
    public float WaterConsumption = 0.5f;
}

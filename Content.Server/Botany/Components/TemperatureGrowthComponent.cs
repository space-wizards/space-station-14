namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class TemperatureGrowthComponent : PlantGrowthComponent
{
    [DataField]
    public float IdealHeat = 293f;

    [DataField]
    public float HeatTolerance = 10f;
}

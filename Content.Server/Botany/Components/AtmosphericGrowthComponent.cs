namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class AtmosphericGrowthComponent : PlantGrowthComponent
{
    [DataField("idealHeat")]
    public float IdealHeat = 293f;

    [DataField("heatTolerance")]
    public float HeatTolerance = 10f;

    [DataField("lowPressureTolerance")]
    public float LowPressureTolerance = 81f;

    [DataField("lighPressureTolerance")]
    public float HighPressureTolerance = 121f;
}

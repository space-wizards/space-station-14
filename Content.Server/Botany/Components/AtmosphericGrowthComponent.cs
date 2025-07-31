namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class AtmosphericGrowthComponent : PlantGrowthComponent
{
    [DataField]
    public float IdealHeat = 293f;

    [DataField]
    public float HeatTolerance = 10f;

    [DataField]
    public float LowPressureTolerance = 81f;

    [DataField]
    public float HighPressureTolerance = 121f;
}

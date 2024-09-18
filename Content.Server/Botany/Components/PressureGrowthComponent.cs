namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class PressureGrowthComponent : PlantGrowthComponent
{
    [DataField]
    public float LowPressureTolerance = 81f;

    [DataField]
    public float HighPressureTolerance = 121f;
}

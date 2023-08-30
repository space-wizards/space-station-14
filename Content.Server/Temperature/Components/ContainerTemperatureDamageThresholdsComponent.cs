namespace Content.Server.Temperature.Components;

[RegisterComponent]
public sealed partial class ContainerTemperatureDamageThresholdsComponent: Component
{
    [DataField("heatDamageThreshold")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float? HeatDamageThreshold;

    [DataField("coldDamageThreshold")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float? ColdDamageThreshold;
}

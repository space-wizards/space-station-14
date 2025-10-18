namespace Content.Server.Temperature.Components;

[RegisterComponent]
public sealed partial class ContainerTemperatureDamageThresholdsComponent: Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? HeatDamageThreshold;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ColdDamageThreshold;
}

namespace Content.Server.Temperature.Components;

[RegisterComponent]
public sealed partial class ContainerTemperatureComponent: Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? HeatDamageThreshold;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float? ColdDamageThreshold;
}

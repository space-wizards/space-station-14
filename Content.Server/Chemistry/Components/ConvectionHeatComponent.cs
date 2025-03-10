namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class ConvectionHeatComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TempDifference;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "beaker";
}

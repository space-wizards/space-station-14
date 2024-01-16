namespace Content.Server.Power.Generator;

[RegisterComponent]
public sealed partial class GeneratorSignalControlComponent: Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string OnPort = "On";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string OffPort = "Off";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string TogglePort = "Toggle";
}

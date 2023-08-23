namespace Content.Server.SS220.AutoEngrave;

[RegisterComponent]
public sealed partial class AutoEngravingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("autoEngraveLocKey")]
    public string? AutoEngraveLocKey;
    [ViewVariables(VVAccess.ReadWrite), DataField("engravedText")]
    public string EngravedText = "";
}

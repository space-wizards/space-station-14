namespace Content.Server.SS220.AutoEngrave;

[RegisterComponent]
public sealed partial class EngraveNameOnOpenComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("activated")]
    public bool Activated;

    [ViewVariables(VVAccess.ReadWrite), DataField("autoEngraveLocKey")]
    public string? AutoEngraveLocKey;

    [ViewVariables(VVAccess.ReadWrite), DataField("toEngrave")]
    public HashSet<string> ToEngrave = new();
}

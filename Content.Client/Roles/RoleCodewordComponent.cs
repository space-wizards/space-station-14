namespace Content.Client.Roles;

/// <summary>
/// 
/// </summary>
[RegisterComponent]
public sealed partial class RoleCodewordComponent : Component
{
    [DataField("codewords"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> Codewords;
}

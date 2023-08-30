namespace Content.Server.Roles;

[RegisterComponent]
public sealed partial class TraitorRoleComponent : AntagonistRoleComponent
{
    [DataField("briefing"), ViewVariables(VVAccess.ReadWrite)]
    public string? Briefing;
}

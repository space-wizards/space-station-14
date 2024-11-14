using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Adds a briefing to the character info menu, does nothing else.
/// </summary>
[RegisterComponent]
public sealed partial class RoleBriefingComponent : BaseRoleComponent
{
    // [DataField("briefing"), ViewVariables(VVAccess.ReadWrite)]
    // public string Briefing = ""; TODO:ERRANT test if the new one still works perfectly

    [DataField]
    public string Briefing;
}

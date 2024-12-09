using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Adds a briefing to the character info menu, does nothing else.
/// </summary>
[RegisterComponent]
public sealed partial class RoleBriefingComponent : BaseRoleComponent
{
    [DataField]
    public string Briefing;
}

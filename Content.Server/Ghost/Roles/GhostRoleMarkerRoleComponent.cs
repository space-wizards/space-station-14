using Content.Shared.Roles;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// This is used for round end display of ghost roles.
/// It may also be used to ensure some ghost roles count as antagonists in future. TODO:ERRANT updgrade text
/// </summary>
[RegisterComponent]
public sealed partial class GhostRoleMarkerRoleComponent : BaseRoleComponent
{
    [DataField] public string? Name;

}

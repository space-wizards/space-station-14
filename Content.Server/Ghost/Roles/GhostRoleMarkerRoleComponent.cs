using Content.Shared.Roles;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// This is used for round end display of ghost roles.
/// It also inherits RoleType, which can be used to set an initial RoleType protoID for the ghostrole
/// </summary>
[RegisterComponent]
public sealed partial class GhostRoleMarkerRoleComponent : BaseRoleComponent
{
    [DataField] public string? Name;

}

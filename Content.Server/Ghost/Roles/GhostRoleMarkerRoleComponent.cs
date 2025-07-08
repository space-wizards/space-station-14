using Content.Shared.Roles;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// Added to mind role entities to tag that they are a ghostrole.
/// It also holds the name for the round end display
/// </summary>
[RegisterComponent]
public sealed partial class GhostRoleMarkerRoleComponent : BaseMindRoleComponent
{
    //TODO does anything still use this? It gets populated by GhostRolesystem but I don't see anything ever reading it
    [DataField] public string? Name;

}

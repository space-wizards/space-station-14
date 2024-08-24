using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are an initial infected.
/// </summary>
[RegisterComponent]
public sealed partial class InitialInfectedRoleComponent : BaseMindRoleComponent
{

}

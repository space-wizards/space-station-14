using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a syndicate traitor.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorRoleComponent : BaseMindRoleComponent
{
}

using Content.Server.Dragon;
using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a space dragon.
/// </summary>
[RegisterComponent, Access(typeof(DragonSystem))]
public sealed partial class DragonRoleComponent : BaseMindRoleComponent
{
}

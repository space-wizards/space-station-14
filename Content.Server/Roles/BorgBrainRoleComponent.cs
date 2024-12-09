using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a brain in an MMI.
/// </summary>
[RegisterComponent]
public sealed partial class BorgBrainRoleComponent : BaseMindRoleComponent
{
}

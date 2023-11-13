using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind entities to tag that they are a pirate.
/// </summary>
[RegisterComponent]
public sealed partial class PiratesRoleComponent : AntagonistRoleComponent
{
}

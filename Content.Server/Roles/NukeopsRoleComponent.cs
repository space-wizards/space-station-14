using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind entities to tag that they are a nuke operative.
/// </summary>
[RegisterComponent, ExclusiveAntagonist]
public sealed partial class NukeopsRoleComponent : AntagonistRoleComponent
{
}

using Robust.Shared.GameStates;

namespace Content.Shared.Roles;

/// <summary>
///     Added to mind entities to tag that they are a nuke operative.
/// </summary>
[RegisterComponent, ExclusiveAntagonist, NetworkedComponent]
public sealed partial class NukeopsRoleComponent : AntagonistRoleComponent
{
}

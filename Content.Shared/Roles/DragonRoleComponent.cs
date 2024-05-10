using Robust.Shared.GameStates;

namespace Content.Shared.Roles;

/// <summary>
/// Role used to keep track of space dragons for antag purposes.
/// </summary>
[RegisterComponent, ExclusiveAntagonist, NetworkedComponent]
public sealed partial class DragonRoleComponent : AntagonistRoleComponent
{
}

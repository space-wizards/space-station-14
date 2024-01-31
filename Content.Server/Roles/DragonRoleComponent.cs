using Content.Server.Dragon;
using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Role used to keep track of space dragons for antag purposes.
/// </summary>
[RegisterComponent, Access(typeof(DragonSystem)), ExclusiveAntagonist]
public sealed partial class DragonRoleComponent : AntagonistRoleComponent
{
}

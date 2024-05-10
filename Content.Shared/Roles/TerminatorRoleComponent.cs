using Robust.Shared.GameStates;

namespace Content.Shared.Roles;

[RegisterComponent, ExclusiveAntagonist, NetworkedComponent]
public sealed partial class TerminatorRoleComponent : AntagonistRoleComponent
{
}

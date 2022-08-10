using Robust.Shared.GameStates;

namespace Content.Shared.Administration;

[RegisterComponent, Access(typeof(AdminFrozenSystem))]
[NetworkedComponent]
public sealed class AdminFrozenComponent : Component
{
}

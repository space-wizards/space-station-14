using Robust.Shared.GameStates;

namespace Content.Shared.Administration;

[RegisterComponent, Friend(typeof(AdminFrozenSystem))]
[NetworkedComponent]
public sealed class AdminFrozenComponent : Component
{
}

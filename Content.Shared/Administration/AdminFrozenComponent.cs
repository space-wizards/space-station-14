using Robust.Shared.GameStates;

namespace Content.Shared.Administration;

[RegisterComponent, Access(typeof(AdminFrozenSystem))]
[NetworkedComponent]
public sealed partial class AdminFrozenComponent : Component
{
}

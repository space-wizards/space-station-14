using Content.Shared.Wires;
using Robust.Shared.GameStates;

namespace Content.Shared.Lock;

/// <summary>
/// This is used for a <see cref="WiresPanelComponent"/> that cannot be opened while locked.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LockSystem))]
public sealed partial class LockedWiresPanelComponent : Component
{

}

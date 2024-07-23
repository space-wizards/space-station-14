using Content.Shared.Construction.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Lock;

/// <summary>
/// This is used for a <see cref="AnchorableComponent"/> that cannot be unanchored while locked.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LockSystem))]
public sealed partial class LockedAnchorableComponent : Component
{

}

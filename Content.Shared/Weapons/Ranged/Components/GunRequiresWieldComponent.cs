using Content.Shared.Wieldable;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Indicates that this gun requires wielding to be useable.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(WieldableSystem))]
public sealed class GunRequiresWieldComponent : Component
{

}

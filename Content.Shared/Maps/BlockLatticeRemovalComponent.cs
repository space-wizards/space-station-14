using Robust.Shared.GameStates;

namespace Content.Shared.Maps;

/// <summary>
/// Prevents lattice from being removed from the grid this component is attached to.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockLatticeRemovalComponent : Component
{

}

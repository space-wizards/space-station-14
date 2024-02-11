using Robust.Shared.GameStates;

namespace Content.Shared.Tiles;

/// <summary>
/// Prevents floor tile updates when attached to a grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProtectedGridComponent : Component
{

}

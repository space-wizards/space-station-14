using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Blocks all attempts to access solutions contained by this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockSolutionAccessComponent : Component
{
}

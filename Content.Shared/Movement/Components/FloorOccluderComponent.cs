using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Applies floor occlusion to any <see cref="FloorOcclusionComponent"/> that intersect us.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FloorOccluderComponent : Component
{

}

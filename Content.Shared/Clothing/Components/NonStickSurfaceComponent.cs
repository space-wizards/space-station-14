using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     Prevent sticky reagents (E.g lube or glue) from sticking
///     to it. If they are applied they will fall to the ground!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NonStickSurfaceComponent : Component
{
}

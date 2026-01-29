using Robust.Shared.GameStates;

namespace Content.Shared.DeepFryer.Components;

/// <summary>
/// Added to fried items to ensure they will not be fried again, to mostly brick them, and to give them the unique "fried" shader
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeenFriedComponent : Component
{
}

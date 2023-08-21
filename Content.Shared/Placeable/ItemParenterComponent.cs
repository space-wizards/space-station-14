using Robust.Shared.GameStates;

namespace Content.Shared.Placeable;

/// <summary>
/// Parents items to it when they are placed, so they stick to it when picked up or moved.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemParenterComponent : Component
{
}

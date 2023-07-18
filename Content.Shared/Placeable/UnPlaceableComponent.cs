using Robust.Shared.GameStates;

namespace Content.Shared.Placeable;

/// <summary>
/// Forbidden to be placed on <see cref="PlaceableSurfaceComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class UnPlaceableComponent : Component
{
}

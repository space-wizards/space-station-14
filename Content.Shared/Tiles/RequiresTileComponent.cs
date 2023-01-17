using Robust.Shared.GameStates;

namespace Content.Shared.Tiles;

/// <summary>
/// Deletes the entity if the tile changes from under it. Used for visual effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class RequiresTileComponent : Component
{

}

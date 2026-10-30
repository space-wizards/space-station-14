using Robust.Shared.GameStates;

namespace Content.Shared.Wall;

/// <summary>
/// Marks the entity as a wall.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WallComponent : Component;

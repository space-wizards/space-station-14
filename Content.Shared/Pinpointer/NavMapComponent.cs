using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Used to store grid poly data to be used for UIs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class NavMapComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<Vector2i, NavMapChunk> Chunks = new();
}

public sealed class NavMapChunk
{
    public readonly Vector2i Origin;

    /// <summary>
    /// Bitmask for tiles, 1 for occupied and 0 for empty.
    /// </summary>
    public int TileData;

    public NavMapChunk(Vector2i origin)
    {
        Origin = origin;
    }
}

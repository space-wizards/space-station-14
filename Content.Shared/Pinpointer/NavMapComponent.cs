using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Used to store grid poly data to be used for UIs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapComponent : Component
{
    /*
     * Don't need DataFields as this can be reconstructed
     */

    [ViewVariables]
    public readonly Dictionary<Vector2i, NavMapChunk> Chunks = new();

    [ViewVariables] public readonly List<SharedNavMapSystem.NavMapBeacon> Beacons = new();

    [ViewVariables] public readonly List<SharedNavMapSystem.NavMapAirlock> Airlocks = new();
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

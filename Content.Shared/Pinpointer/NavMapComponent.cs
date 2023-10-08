using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Power;

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

[Serializable, NetSerializable]
public sealed class NavMapChunkPowerCables
{
    public readonly Vector2i Origin;

    /// <summary>
    /// Bitmask for tiles, 1 for occupied and 0 for empty.
    /// </summary>
    public Dictionary<CableType, int> CableData;

    public NavMapChunkPowerCables(Vector2i origin)
    {
        Origin = origin;
        CableData = new Dictionary<CableType, int>();
    }
}

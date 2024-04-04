using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Used to store grid data to be used for UIs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapComponent : Component
{
    /*
     * Don't need DataFields as this can be reconstructed
     */

    /// <summary>
    /// Bitmasks that represent chunked tiles.
    /// </summary>
    [ViewVariables]
    public Dictionary<(NavMapChunkType, Vector2i), NavMapChunk> Chunks = new();

    /// <summary>
    /// List of station beacons.
    /// </summary>
    [ViewVariables]
    public HashSet<SharedNavMapSystem.NavMapBeacon> Beacons = new();
}

[Serializable, NetSerializable]
public sealed class NavMapChunk
{
    /// <summary>
    /// The chunk origin
    /// </summary>
    public readonly Vector2i Origin;

    /// <summary>
    /// Bitmask for tiles, 1 for occupied and 0 for empty. There is a bitmask for each cardinal direction,
    /// representing each edge of the tile, in case the entities inside it do not entirely fill it
    /// </summary>
    public Dictionary<AtmosDirection, ushort> TileData;

    /// <summary>
    /// The last game tick that the chunk was updated
    /// </summary>
    [NonSerialized]
    public GameTick LastUpdate;

    public NavMapChunk(Vector2i origin)
    {
        Origin = origin;

        TileData = new()
        {
            [AtmosDirection.North] = 0,
            [AtmosDirection.East] = 0,
            [AtmosDirection.South] = 0,
            [AtmosDirection.West] = 0,
        };
    }
}

public enum NavMapChunkType : byte
{
    Invalid,
    Floor,
    Wall,
    Airlock,
}

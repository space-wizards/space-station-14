using System.Linq;
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
    public Dictionary<Vector2i, NavMapChunk> Chunks = new();

    /// <summary>
    /// List of station beacons.
    /// </summary>
    [ViewVariables]
    public Dictionary<NetEntity, SharedNavMapSystem.NavMapBeacon> Beacons = new();
}

[Serializable, NetSerializable]
public sealed class NavMapChunk(Vector2i origin)
{
    /// <summary>
    /// The chunk origin
    /// </summary>
    [ViewVariables]
    public readonly Vector2i Origin = origin;

    /// <summary>
    /// Array containing the chunk's data. The
    /// </summary>
    [ViewVariables]
    public int[] TileData = new int[SharedNavMapSystem.ArraySize];

    /// <summary>
    /// The last game tick that the chunk was updated
    /// </summary>
    [NonSerialized]
    public GameTick LastUpdate;
}

public enum NavMapChunkType : byte
{
    // Values represent bit shift offsets when retrieving data in the tile array.
    Invalid  = byte.MaxValue,
    Floor = 0, // I believe floors have directional information for diagonal tiles?
    Wall = SharedNavMapSystem.Directions,
    Airlock = 2 * SharedNavMapSystem.Directions,
}


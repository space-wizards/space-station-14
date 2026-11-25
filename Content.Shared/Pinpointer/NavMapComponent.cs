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

    /// <summary>
    /// Describes the properties of a region on the station.
    /// It is indexed by the entity assigned as the region owner.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<NetEntity, SharedNavMapSystem.NavMapRegionProperties> RegionProperties = new();

    /// <summary>
    /// All flood filled regions, ready for display on a NavMapControl.
    /// It is indexed by the entity assigned as the region owner.
    /// </summary>
    /// <remarks>
    /// For client use only
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<NetEntity, NavMapRegionOverlay> RegionOverlays = new();

    /// <summary>
    /// A queue of all region owners that are waiting their associated regions to be floodfilled.
    /// </summary>
    /// <remarks>
    /// For client use only
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Queue<NetEntity> QueuedRegionsToFlood = new();

    /// <summary>
    /// A look up table to get a list of region owners associated with a flood filled chunk.
    /// </summary>
    /// <remarks>
    /// For client use only
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<Vector2i, HashSet<NetEntity>> ChunkToRegionOwnerTable = new();

    /// <summary>
    ///  A look up table to find flood filled chunks associated with a given region owner.
    /// </summary>
    /// <remarks>
    /// For client use only
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<NetEntity, HashSet<Vector2i>> RegionOwnerToChunkTable = new();
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

[Serializable, NetSerializable]
public sealed class NavMapRegionOverlay(Enum uiKey, List<(Vector2i, Vector2i)> gridCoords)
{
    /// <summary>
    /// The key to the UI that will be displaying this region on its navmap
    /// </summary>
    public Enum UiKey = uiKey;

    /// <summary>
    /// The local grid coordinates of the rectangles that make up the region
    /// Item1 is the top left corner, Item2 is the bottom right corner
    /// </summary>
    public List<(Vector2i, Vector2i)> GridCoords = gridCoords;

    /// <summary>
    /// Color of the region
    /// </summary>
    public Color Color = Color.White;
}

public enum NavMapChunkType : byte
{
    // Values represent bit shift offsets when retrieving data in the tile array.
    Invalid = byte.MaxValue,
    Floor = 0, // I believe floors have directional information for diagonal tiles?
    Wall = SharedNavMapSystem.Directions,
    Airlock = 2 * SharedNavMapSystem.Directions,
}


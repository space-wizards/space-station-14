using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/*
 * I bikeshedded a lot on how to do this and I'm still not entirely happy.
 * The main thing is you need a weak ref to the poly because it may be invalidated due to graph updates.
 * I had a struct version but you still need to store the neighbors somewhere, maybe on the chunk itself?
 * Future dev work required.
 */

/// <summary>
/// A path poly to be used for networked debug purposes.
/// </summary>
[Serializable, NetSerializable]
public sealed class DebugPathPoly
{
    public NetEntity GraphUid;
    public Vector2i ChunkOrigin;
    public byte TileIndex;

    public Box2 Box;
    public PathfindingData Data;
    public List<NetCoordinates> Neighbors = default!;
}

[Serializable, NetSerializable]
public sealed class DebugPathPolyNeighbor
{
    public NetCoordinates Coordinates;
}

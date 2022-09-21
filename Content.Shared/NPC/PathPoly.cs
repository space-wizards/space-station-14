using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/*
 * I bikeshedded a lot on how to do this and I'm still not entirely happy.
 * The main thing is you need a weak ref to the poly because it may be invalidated due to graph updates.
 */

[Serializable, NetSerializable]
public struct PathPoly : IEquatable<PathPoly>
{
    public Box2 Box;
    public PathfindingData Data;
    public HashSet<PathPolyRef> Neighbors;

    public PathPoly(Box2 vertices, PathfindingData data)
    {
        Box = vertices;
        Data = data;
        Neighbors = new HashSet<PathPolyRef>();
    }

    // Neighbors deliberately ignored.

    public bool Equals(PathPoly other)
    {
        return Data.Equals(other.Data) &&
               Box.Equals(other.Box) &&
               Neighbors.SetEquals(other.Neighbors);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data, Box, Neighbors);
    }
}

[Serializable, NetSerializable]
public struct PathPolyRef : IEquatable<PathPolyRef>
{
    /// <summary>
    /// Graph that this is the reference for.
    /// </summary>
    public EntityUid GraphUid;

    public Vector2i ChunkOrigin;

    /// <summary>
    /// X / Y index of the tile in the chunk.
    /// </summary>
    public byte Index;

    /// <summary>
    /// Hash of the target poly.
    /// </summary>
    public int Hash;

    public PathPolyRef(EntityUid graphUid, Vector2i chunkOrigin, byte index, int hash)
    {
        GraphUid = graphUid;
        ChunkOrigin = chunkOrigin;
        Index = index;
    }

    public bool Equals(PathPolyRef other)
    {
        return Hash.Equals(other.Hash) &&
               GraphUid.Equals(other.GraphUid) &&
               ChunkOrigin.Equals(other.ChunkOrigin) &&
               Index == other.Index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GraphUid, ChunkOrigin, Index, Hash);
    }
}

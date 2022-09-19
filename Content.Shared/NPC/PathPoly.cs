using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public struct PathPoly : IEquatable<PathPoly>
{
    public Box2 Box;
    public PathfindingData Data;
    public List<PathPolyRef> Neighbors;

    public PathPoly(Box2 vertices, PathfindingData data)
    {
        Box = vertices;
        Data = data;
        Neighbors = new List<PathPolyRef>();
    }

    public bool Equals(PathPoly other)
    {
        return Data.Equals(other.Data) &&
               Box.Equals(other.Box) &&
               Neighbors.Equals(other.Neighbors);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data, Box, Neighbors);
    }
}

[Serializable, NetSerializable]
public struct PathPolyRef : IEquatable<PathPolyRef>
{
    public Vector2i ChunkOrigin;

    /// <summary>
    /// X / Y index of the tile in the chunk.
    /// </summary>
    public byte Index;

    /// <summary>
    /// Index of the poly on the tile's polys.
    /// </summary>
    public byte TileIndex;

    public bool Equals(PathPolyRef other)
    {
        return ChunkOrigin.Equals(other.ChunkOrigin) &&
               Index == other.Index &&
               TileIndex == other.TileIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ChunkOrigin, Index, TileIndex);
    }
}

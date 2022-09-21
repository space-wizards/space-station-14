using Content.Server.NPC.Pathfinding;
using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/*
 * I bikeshedded a lot on how to do this and I'm still not entirely happy.
 * The main thing is you need a weak ref to the poly because it may be invalidated due to graph updates.
 * I had a struct version but you still need to store the neighbors somewhere, maybe on the chunk itself?
 * Future dev work required.
 */

[Serializable, NetSerializable]
public sealed class PathPoly : IEquatable<PathPoly>
{
    public readonly EntityUid GraphUid;
    [NonSerialized]
    public readonly GridPathfindingChunk Chunk;
    public readonly byte TileIndex;

    public readonly Box2 Box;
    public PathfindingData Data;
    public readonly HashSet<PathPoly> Neighbors;

    public PathPoly(EntityUid graphUid, GridPathfindingChunk chunk, byte tileIndex, Box2 vertices, PathfindingData data, HashSet<PathPoly> neighbors)
    {
        GraphUid = graphUid;
        Chunk = chunk;
        TileIndex = tileIndex;
        Box = vertices;
        Data = data;
        Neighbors = neighbors;
    }

    public bool IsValid()
    {
        return (Data.Flags & PathfindingBreadcrumbFlag.Invalid) == 0x0;
    }

    public bool Equals(PathPoly? other)
    {
        return other != null &&
               GraphUid.Equals(other.GraphUid) &&
               Chunk.Equals(other.Chunk) &&
               TileIndex == other.TileIndex &&
               Data.Equals(other.Data) &&
               Box.Equals(other.Box) &&
               Neighbors.SetEquals(other.Neighbors);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is PathPoly other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GraphUid, Chunk, TileIndex, Box);
    }
}

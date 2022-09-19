using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public struct PathPoly : IEquatable<PathPoly>
{
    public Box2 Box;
    public PathfindingData Data;
    public HashSet<PathPoly> Neighbors;

    public PathPoly(Box2 vertices, PathfindingData data)
    {
        Box = vertices;
        Data = data;
        Neighbors = new();
    }

    public bool Equals(PathPoly other)
    {
        return Data.Equals(other.Data) &&
               Box.Equals(other.Box) &&
               Neighbors.Equals(other.Neighbors);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathPoly other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data, Box, Neighbors);
    }
}

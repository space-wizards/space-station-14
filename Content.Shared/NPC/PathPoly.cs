using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public struct PathPoly : IEquatable<PathPoly>
{
    public Vector2i[] Vertices;
    public PathfindingData Data;
    public HashSet<PathPoly> Neighbors = new();

    public PathPoly(Vector2i[] vertices, PathfindingData data)
    {
        Vertices = vertices;
        Data = data;
    }

    public bool Equals(PathPoly other)
    {
        return Data.Equals(other.Data) &&
               Vertices.Length.Equals(other.Vertices.Length) &&
               Vertices.SequenceEqual(other.Vertices) &&
               Neighbors.Equals(other.Neighbors);
    }

    public override bool Equals(object? obj)
    {
        return obj is PathPoly other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Vertices, Data, Neighbors);
    }
}

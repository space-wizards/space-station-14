namespace Content.Shared.Collections;

public readonly struct NodeId : IEquatable<NodeId>
{
    public readonly int Index;
    public readonly int Generation;

    public long Combined => (uint) Index | ((long) Generation << 32);

    public NodeId(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    public NodeId(long combined)
    {
        Index = (int) combined;
        Generation = (int) (combined >> 32);
    }

    public bool Equals(NodeId other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is NodeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    public static bool operator ==(NodeId left, NodeId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NodeId left, NodeId right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Index} (G{Generation})";
    }
}

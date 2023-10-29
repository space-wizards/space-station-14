using Robust.Shared.Timing;

namespace Content.Server.Nodes;

/// <summary>
/// A marker for the last time a node or node graph updated (split or merged) including tick and subtick precision.
/// </summary>
[DataDefinition]
public partial struct UpdateIter : IComparable<UpdateIter>, IEquatable<UpdateIter>
{
    /// <summary>The earliest possible iteration.</summary>
    [ViewVariables]
    public static readonly UpdateIter MinValue = new(GameTick.Zero, int.MinValue);
    /// <summary>The latest possible iteration.</summary>
    [ViewVariables]
    public static readonly UpdateIter MaxValue = new(GameTick.MaxValue, int.MaxValue);


    /// <summary>The time of the update when this iteration occurred.</summary>
    [DataField]
    public GameTick Tick { get; init; }

    /// <summary>The iteration within the tick that this represents.</summary>
    [DataField]
    public int Iter { get; init; }


    /// <summary>Constructs a new iteration marker from a tick time and subtick iteration.</summary>
    public UpdateIter(GameTick tick, int iter)
    {
        Tick = tick;
        Iter = iter;
    }

    /// <summary>Extracts the tick time and subtick iteration this represents.</summary>
    public void Deconstruct(out GameTick tick, out int iter)
    {
        tick = Tick;
        iter = Iter;
    }

    public override string ToString()
    {
        return $"{Tick}: {Iter}";
    }

    public bool Equals(UpdateIter other)
    {
        return Tick.Equals(other.Tick) && Iter.Equals(other.Iter);
    }

    public override bool Equals(object? other)
    {
        return other is UpdateIter otherIter && Equals(otherIter);
    }

    public int CompareTo(UpdateIter other)
    {
        var priority = Tick.CompareTo(other.Tick);
        return priority != 0 ? priority : Iter.CompareTo(other.Iter);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Tick, Iter);
    }

    public static bool operator ==(UpdateIter a, UpdateIter b) => a.Equals(b);
    public static bool operator !=(UpdateIter a, UpdateIter b) => !a.Equals(b);
    public static bool operator <=(UpdateIter a, UpdateIter b) => a.CompareTo(b) <= 0;
    public static bool operator >=(UpdateIter a, UpdateIter b) => a.CompareTo(b) >= 0;
    public static bool operator <(UpdateIter a, UpdateIter b) => a.CompareTo(b) < 0;
    public static bool operator >(UpdateIter a, UpdateIter b) => a.CompareTo(b) > 0;
}

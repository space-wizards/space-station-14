namespace Content.Server.Nodes;

/// <summary>
/// A marker for the last time a node or node graph updated (split or merged) including tick and subtick precision.
/// </summary>
[DataDefinition]
public partial struct UpdateIter : IComparable<UpdateIter>, IEquatable<UpdateIter>
{
    /// <summary>The earliest possible iteration.</summary>
    [ViewVariables]
    public static readonly UpdateIter MinValue = new(TimeSpan.MinValue, int.MinValue);
    /// <summary>The latest possible iteration.</summary>
    [ViewVariables]
    public static readonly UpdateIter MaxValue = new(TimeSpan.MaxValue, int.MaxValue);


    /// <summary>The time of the update when this iteration occurred.</summary>
    [DataField("time")]
    public TimeSpan Time { get; init; }

    /// <summary>The iteration within the tick that this represents.</summary>
    [DataField("iter")]
    public int Iter { get; init; }


    /// <summary>Constructs a new iteration marker from a tick time and subtick iteration.</summary>
    public UpdateIter(TimeSpan time, int iter)
    {
        Time = time;
        Iter = iter;
    }

    /// <summary>Extracts the tick time and subtick iteration this represents.</summary>
    public void Deconstruct(out TimeSpan time, out int iter)
    {
        time = Time;
        iter = Iter;
    }

    public override string ToString()
    {
        return $"{Time}: {Iter}";
    }

    public bool Equals(UpdateIter other)
    {
        return Time.Equals(other.Time) && Iter.Equals(other.Iter);
    }

    public override bool Equals(object? other)
    {
        return other is UpdateIter otherIter && Equals(otherIter);
    }

    public int CompareTo(UpdateIter other)
    {
        var priority = Time.CompareTo(other.Time);
        return priority != 0 ? priority : Iter.CompareTo(other.Iter);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Time.GetHashCode(), Iter);
    }

    public static bool operator ==(UpdateIter a, UpdateIter b) => a.Equals(b);
    public static bool operator !=(UpdateIter a, UpdateIter b) => !a.Equals(b);
    public static bool operator <=(UpdateIter a, UpdateIter b) => a.CompareTo(b) <= 0;
    public static bool operator >=(UpdateIter a, UpdateIter b) => a.CompareTo(b) >= 0;
    public static bool operator <(UpdateIter a, UpdateIter b) => a.CompareTo(b) < 0;
    public static bool operator >(UpdateIter a, UpdateIter b) => a.CompareTo(b) > 0;
}

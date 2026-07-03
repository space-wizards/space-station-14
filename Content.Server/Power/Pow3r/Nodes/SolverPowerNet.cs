using Content.Server.Collections;
using Content.Shared.Power.Pow3r;

namespace Content.Server.Power.Pow3r.Nodes;

public struct SolverPowerNetwork : IPowerNetwork, IEquatable<SolverPowerNetwork>
{
    public SolverPowerNetwork()
    {
    }



    [ViewVariables]
    public HashSet<EntityUid> Chargers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Dischargers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Consumers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Suppliers { get; set; } = new();

    [ViewVariables]
    public NodeId Id { get; set; } = default;

    /// <summary>
    ///     Power generators
    /// </summary>
    [ViewVariables]
    public List<NodeId> Supplies { get; set; } = new();

    /// <summary>
    ///     Power consumers.
    /// </summary>
    [ViewVariables]
    public List<NodeId> Loads { get; set; } = new();

    /// <summary>
    ///     Batteries that are draining power from this network (connected to the INPUT port of the battery).
    /// </summary>
    [ViewVariables]
    public List<NodeId> BatteryLoads { get; set; } = new();

    /// <summary>
    ///     Batteries that are supplying power to this network (connected to the OUTPUT port of the battery).
    /// </summary>
    [ViewVariables]
    public List<NodeId> BatterySupplies { get; set; } = new();

    [ViewVariables]
    public float LastCombinedLoad { get; set; } = 0;

    [ViewVariables]
    public float LastCombinedSupply { get; set; } = 0;

    [ViewVariables]
    public float LastCombinedMaxSupply { get; set; } = 0;

    [ViewVariables]
    public int Height { get; set; } = 0;

    public bool Equals(SolverPowerNetwork other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SolverPowerNetwork other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(SolverPowerNetwork left, SolverPowerNetwork right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SolverPowerNetwork left, SolverPowerNetwork right)
    {
        return !(left == right);
    }
}

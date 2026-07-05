using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r;

public struct PowerNetwork : IPowerNetwork, IEquatable<PowerNetwork>
{
    public PowerNetwork()
    {
    }

    [ViewVariables]
    public float LastCombinedLoad { get; set; } = 0;

    [ViewVariables]
    public float LastCombinedSupply { get; set; } = 0;

    [ViewVariables]
    public float LastCombinedMaxSupply { get; set; } = 0;

    [ViewVariables]
    public int Height { get; set; } = 0;

    [ViewVariables]
    public NodeId Id { get; set; } = default;

    [ViewVariables]
    public List<NodeId> Supplies { get; set; } = new();

    [ViewVariables]
    public List<NodeId> Loads { get; set; } = new();

    [ViewVariables]
    public List<NodeId> BatteryLoads { get; set; } = new();

    [ViewVariables]
    public List<NodeId> BatterySupplies { get; set; } = new();

    public bool Equals(PowerNetwork other)
    {
        return Height == other.Height && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is PowerNetwork other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Height, Id);
    }

    public static bool operator ==(PowerNetwork left, PowerNetwork right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PowerNetwork left, PowerNetwork right)
    {
        return !left.Equals(right);
    }
}

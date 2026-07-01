using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r;

public interface IPowerNetwork
{
    NodeId Id { get; set; }

    /// <summary>
    ///     Power generators
    /// </summary>
    List<NodeId> Supplies { get; set; }

    /// <summary>
    ///     Power consumers.
    /// </summary>
    List<NodeId> Loads { get; set; }

    /// <summary>
    ///     Batteries that are draining power from this network (connected to the INPUT port of the battery).
    /// </summary>
    List<NodeId> BatteryLoads { get; set; }

    /// <summary>
    ///     Batteries that are supplying power to this network (connected to the OUTPUT port of the battery).
    /// </summary>
    List<NodeId> BatterySupplies { get; set; }

    /// <summary>
    ///     The total load on the power network as of last tick.
    /// </summary>
    float LastCombinedLoad { get; set; }

    /// <summary>
    ///     Available supply, including both normal supplies and batteries.
    /// </summary>
    float LastCombinedSupply { get; set; }

    /// <summary>
    ///     Theoretical maximum supply, including both normal supplies and batteries.
    /// </summary>
    float LastCombinedMaxSupply { get; set; }

    int Height { get; set; }
}

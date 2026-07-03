namespace Content.Shared.Power.Pow3r;

public interface IPowerNetwork
{
    HashSet<EntityUid> Chargers { get; set; }

    HashSet<EntityUid> Dischargers { get; set; }

    HashSet<EntityUid> Consumers { get; set; }

    HashSet<EntityUid> Suppliers { get; set; }

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

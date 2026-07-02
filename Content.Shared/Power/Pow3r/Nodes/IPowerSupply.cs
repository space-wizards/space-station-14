using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public interface IPowerSupply : IPowerNode
{
    float MaxSupply { get; set; }

    float SupplyRampRate { get; set; }

    float SupplyRampTolerance { get; set; }

    // == Runtime parameters ==

    /// <summary>
    ///     Actual power supplied last network update.
    /// </summary>
    float CurrentSupply { get; set; }

    /// <summary>
    ///     The amount of power we WANT to be supplying to match grid load.
    /// </summary>
    float SupplyRampTarget { get; set; }

    /// <summary>
    ///     Position of the supply ramp.
    /// </summary>
    float SupplyRampPosition { get; set; }

    NodeId LinkedNetwork { get; set; }

    /// <summary>
    ///     Supply available during a tick. The actual current supply will be less than or equal to this. Used
    ///     during calculations.
    /// </summary>
    float AvailableSupply { get; set; }
}

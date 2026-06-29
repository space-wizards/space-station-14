using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public interface IPowerBattery : IPowerNode
{
    bool CanDischarge { get; set; }

    bool CanCharge { get; set; }

    float Capacity { get; set; }

    float MaxChargeRate { get; set; }

    float MaxThroughput { get; set; } // 0 = infinite cuz imgui

    float MaxSupply { get; set; }

    /// <summary>
    ///     The batteries supply ramp tolerance. This is an always available supply added to the ramped supply.
    /// </summary>
    /// <remarks>
    ///     Note that this MUST BE GREATER THAN ZERO, otherwise the current battery ramping calculation will not work.
    /// </remarks>
    float SupplyRampTolerance { get; set; }

    float SupplyRampRate { get; set; }

    float Efficiency { get; set; }

    // == Runtime parameters ==
    float SupplyRampPosition { get; set; }

    float CurrentSupply { get; set; }

    float CurrentStorage { get; set; }

    float CurrentReceiving { get; set; }

    float LoadingNetworkDemand { get; set; }

    bool SupplyingMarked { get; set; }

    bool LoadingMarked { get; set; }

    /// <summary>
    ///     Amount of supply that the battery can provide this tick.
    /// </summary>
    float AvailableSupply { get; set; }

    float DesiredPower { get; set; }

    float SupplyRampTarget { get; set; }

    NodeId LinkedNetworkCharging { get; set; }

    NodeId LinkedNetworkDischarging { get; set; }

    /// <summary>
    ///  Theoretical maximum effective supply, assuming the network providing power to this battery continues to supply it
    ///  at the same rate.
    /// </summary>
    float MaxEffectiveSupply { get; set; }
}

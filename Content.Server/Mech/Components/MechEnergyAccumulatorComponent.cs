using Robust.Shared.GameObjects;

namespace Content.Server.Mech.Components;

/// <summary>
/// Component that manages energy accumulation and recharge for mechs.
/// Allows mechs to accumulate energy over time from various sources
/// and provides a buffer for power consumption.
/// </summary>
[RegisterComponent]
public sealed partial class MechEnergyAccumulatorComponent : Component
{
    /// <summary>
    /// The rate at which energy is being accumulated per second.
    /// This value is set by external systems and represents the current
    /// recharge rate from power sources.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PendingRechargeRate;

    /// <summary>
    /// Current accumulated energy stored in the accumulator.
    /// This energy can be consumed by the mech's systems.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
	public float Current;

    /// <summary>
    /// Maximum capacity of the energy accumulator.
    /// Once this limit is reached, no more energy can be accumulated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
	public float Max;
}

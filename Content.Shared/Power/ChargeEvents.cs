using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.Power;

/// <summary>
/// Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
/// </summary>
[ByRefEvent]
public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);

/// <summary>
/// Event that supports multiple battery types.
/// Raised when it is necessary to get information about battery charges.
/// Works with either <see cref="BatteryComponent"/> or <see cref="PowerCellSlotComponent"/>.
/// If there are multiple batteries then the results will be summed up.
/// </summary>
[ByRefEvent]
public record struct GetChargeEvent
{
    public float CurrentCharge;
    public float MaxCharge;
}

/// <summary>
/// Method event that supports multiple battery types.
/// Raised when it is necessary to change the current battery charge by some value.
/// Works with either <see cref="BatteryComponent"/> or <see cref="PowerCellSlotComponent"/>.
/// If there are multiple batteries then they will be changed in order of subscription until the total value was reached.
/// </summary>
[ByRefEvent]
public record struct ChangeChargeEvent(float Amount)
{
    /// <summary>
    /// The total amount of charge to change the battery's storage by (in joule).
    /// A positive value adds charge, a negative value removes charge.
    /// </summary>
    public readonly float Amount = Amount;

    /// <summary>
    /// The amount of charge that still has to be removed.
    /// For cases where there are multiple batteries.
    /// </summary>
    public float ResidualValue = Amount;
}

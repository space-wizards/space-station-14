using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.Power;

/// <summary>
/// Raised when a battery's charge, charge rate or capacity was updated (capacity affects relative charge percentage).
/// If a battery uses <see cref="BatteryComponent.ChargeRate"/> to (dis)charge this is NOT raised every single tick, but only when the charge rate is updated.
/// For instantaneous charge changes using <see cref="SharedBatterySystem.SetCharge"/>, <see cref="SharedBatterySystem.ChangeCharge"/> or similar this DOES get raised, but
/// you should avoid doing so in update loops if the component has net sync enabled.
/// </summary>
[ByRefEvent]
public readonly record struct ChargeChangedEvent(float CurrentCharge, float Delta, float CurrentChargeRate, float MaxCharge)
{
    /// <summary>
    /// The new charge of the battery.
    /// </summary>
    public readonly float CurrentCharge = CurrentCharge;

    /// <summary>
    /// The amount the charge was changed by.
    /// This might be 0 if only the charge rate was modified.
    /// </summary>
    public readonly float Delta = Delta;

    /// <summary>
    /// The new charge rate of the battery.
    /// </summary>
    public readonly float CurrentChargeRate = CurrentChargeRate;

    /// <summary>
    /// The maximum charge of the battery.
    /// </summary>
    public readonly float MaxCharge = MaxCharge;
}

/// <summary>
/// Raised when a battery changes its state between full, empty, or neither.
/// Useful to detect when a battery is empty or fully charged (since ChargeChangedEvent does not get raised every tick for batteries with a constant charge rate).
/// </summary>
[ByRefEvent]
public record struct BatteryStateChangedEvent(BatteryState OldState, BatteryState NewState);

/// <summary>
/// Raised to calculate a predicted battery's recharge rate.
/// Subscribe to this to offset its current charge rate.
/// </summary>
[ByRefEvent]
public record struct RefreshChargeRateEvent(float MaxCharge)
{
    public readonly float MaxCharge = MaxCharge;
    public float NewChargeRate;
}

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

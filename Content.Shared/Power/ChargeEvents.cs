using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.Power;

/// <summary>
/// Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
/// Only raised for entities with <see cref="BatteryComponent"/>.
/// </summary>
[ByRefEvent]
public readonly record struct ChargeChangedEvent(float Charge, float Delta, float MaxCharge)
{
    /// <summary>
    /// The new charge of the battery.
    /// </summary>
    public readonly float Charge = Charge;

    /// <summary>
    /// The amount the charge was changed by.
    /// </summary>
    public readonly float Delta = Delta;

    /// <summary>
    /// The maximum charge of the battery.
    /// </summary>
    public readonly float MaxCharge = MaxCharge;
}

/// <summary>
/// Raised when a predicted battery's charge or capacity changes (capacity affects relative charge percentage).
/// Unlike <see cref="ChargeChangedEvent"/> this is not raised repeatedly each time the charge changes, but only when the charge rate is changed
/// or a charge amount was added or removed instantaneously. The current charge can be inferred from the time of the last update and the charge and
/// charge rate at that time.
/// Only raised for entities with <see cref="PredictedBatteryComponent"/>.
/// </summary>
[ByRefEvent]
public readonly record struct PredictedBatteryChargeChangedEvent(float CurrentCharge, float Delta, float CurrentChargeRate, float MaxCharge)
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
/// Used only for <see cref="PredictedBatteryComponent"/>.
/// </summary>
[ByRefEvent]
public record struct PredictedBatteryStateChangedEvent(BatteryState OldState, BatteryState NewState);

/// <summary>
/// Raised to calculate a predicted battery's recharge rate.
/// Subscribe to this to offset its current charge rate.
/// Used only for <see cref="PredictedBatteryComponent"/>.
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
/// Works with either <see cref="BatteryComponent"/>, <see cref="PredictedBatteryComponent"/>, or <see cref="PowerCellSlotComponent"/>.
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
/// Works with either <see cref="BatteryComponent"/>, <see cref="PredictedBatteryComponent"/>, or <see cref="PowerCellSlotComponent"/>.
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

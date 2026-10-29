namespace Content.Server.Power.Events;

/// <summary>
/// Raised on an entity when its PowerNetworkBatteryComponent's CanCharge state changes.
/// </summary>
/// <param name="On">The new state of the CanCharge field.</param>
[ByRefEvent]
public record struct PowerNetworkBatteryCanChargeChangedEvent(bool On);

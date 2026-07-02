namespace Content.Server.Power.Events;

/// <summary>
/// Raised on an entity when its PowerNetworkBatteryComponent's CanDischarge state changes.
/// </summary>
/// <param name="On">The new state of the CanDischarge field.</param>
[ByRefEvent]
public record struct PowerNetworkBatteryCanDischargeChangedEvent(bool On);

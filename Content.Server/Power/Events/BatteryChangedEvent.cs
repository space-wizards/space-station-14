namespace Content.Server.Power.Events;

/// <summary>
/// Raised on an entity when a related entity that stores a powercell has its powercell changed.
/// </summary>
[ByRefEvent]
public record struct BatteryChangedEvent(EntityUid Battery, EntityUid BatteryHolder);

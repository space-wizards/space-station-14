namespace Content.Server.Ninja.Events;

/// <summary>
/// Raised on the ninja and suit when the suit has its powercell changed.
/// </summary>
[ByRefEvent]
public record struct NinjaBatteryChangedEvent(EntityUid Battery, EntityUid BatteryHolder);

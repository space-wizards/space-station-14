namespace Content.Server._FTL.ShipTracker.Events;

/// <summary>
/// Raised when a ship is damaged.
/// </summary>
[ByRefEvent]
public readonly record struct ShipDamagedEvent(EntityUid Source, ShipTrackerComponent Component);

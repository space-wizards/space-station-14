using Content.Server._FTL.ShipHealth;
using Robust.Shared.Map;

namespace Content.Server._FTL.ShipTracker.Events;

/// <summary>
/// Raised before a ship is going to be destroyed.
/// </summary>
[ByRefEvent]
public readonly record struct BeforeShipDestroy(ShipTrackerComponent Component);

/// <summary>
/// Raised after a ship is destroyed.
/// </summary>
[ByRefEvent]
public readonly record struct AfterShipDestroy(ShipTrackerComponent Component);

/// <summary>
/// Raised when a ship is about to be destroyed.
/// </summary>
[ByRefEvent]
public readonly record struct ShipDestroyAttempt(ShipTrackerComponent Component, bool Cancelled = false);

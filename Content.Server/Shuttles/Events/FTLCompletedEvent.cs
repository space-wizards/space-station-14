using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Events;

[ByRefEvent]
public readonly record struct FTLStartedEvent;

/// <summary>
/// Raised when <see cref="ShuttleSystem.FasterThanLight"/> has completed FTL Travel.
/// </summary>
[ByRefEvent]
public readonly record struct FTLCompletedEvent;

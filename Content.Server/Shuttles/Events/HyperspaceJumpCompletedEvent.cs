using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when <see cref="ShuttleSystem.FasterThanLight"/> has completed FTL Travel.
/// </summary>
public sealed class HyperspaceJumpCompletedEvent: EntityEventArgs {}

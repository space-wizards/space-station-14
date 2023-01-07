namespace Content.Server.Cargo.Events;

/// <summary>
/// Raised by some entities when they are on a cargo shuttle that gets recalled
/// </summary>
[ByRefEvent]
public record struct RecalledOnShuttleEvent(EntityUid uid);

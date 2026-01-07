namespace Content.Shared.Beeper;

/// <summary>
/// Raised when entity with <see cref="BeeperComponent"/> beeps.
/// </summary>
[ByRefEvent]
public record struct BeepPlayedEvent(bool Muted);

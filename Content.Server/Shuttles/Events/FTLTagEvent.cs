namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when trying to get a priority tag for docking.
/// </summary>
[ByRefEvent]
public record struct FTLTagEvent(bool Handled, string? Tag);

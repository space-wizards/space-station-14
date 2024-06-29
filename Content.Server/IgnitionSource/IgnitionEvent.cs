namespace Content.Server.IgnitionSource;

/// <summary>
///     Raised in order to toggle the ignitionSourceComponent on an entity on or off
/// </summary>
[ByRefEvent]
public record struct IgnitionEvent(bool Ignite = false);

namespace Content.Shared.IgnitionSource;

/// <summary>
///     Raised in order to toggle the ignitionSourceComponent on an entity on or off
/// </summary>
[ByRefEvent]
public readonly record struct IgnitionEvent(bool Ignite = false);

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Raised on a device when its invoke limit is reached.
/// </summary>
[ByRefEvent]
public readonly record struct DeviceLinkOverloadedEvent;

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Raised whenever an entity receives a signal from the <see cref="Trigger"/> entity on some <see cref="Port"/>
/// </summary>
[ByRefEvent]
public readonly record struct SignalReceivedEvent(string Port, EntityUid? Trigger = null);

/// <summary>
/// Raised whenever an entity receives a signal from the <see cref="Trigger"/> entity on some <see cref="Port"/>
/// together with additional <see cref="Data"/> network payload.
/// </summary>
[ByRefEvent]
public readonly record struct SignalReceivedEvent<T>(string Port, T Data, EntityUid? Trigger = null) where T : ISignalNetworkPayload;

namespace Content.Shared.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct SignalReceivedEvent(string Port, EntityUid? Trigger = null);

[ByRefEvent]
public readonly record struct SignalReceivedEvent<T>(string Port, T Data, EntityUid? Trigger = null) where T : ISignalNetworkPayload;

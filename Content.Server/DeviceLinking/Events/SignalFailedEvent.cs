namespace Content.Server.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct SignalFailedEvent(EntityUid? uid);

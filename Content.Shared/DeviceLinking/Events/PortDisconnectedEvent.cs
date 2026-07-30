namespace Content.Shared.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct PortDisconnectedEvent(string Port);

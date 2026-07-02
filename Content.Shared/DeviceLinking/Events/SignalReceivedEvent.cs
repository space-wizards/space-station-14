using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct SignalReceivedEvent(string Port, EntityUid? Trigger = null, INetworkPayload? Data = null);

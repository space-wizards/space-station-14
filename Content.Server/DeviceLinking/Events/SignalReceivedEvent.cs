using Content.Server.DeviceNetwork;

namespace Content.Server.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct SignalReceivedEvent(string Port, EntityUid? Trigger = null, NetworkPayload? Data = null)
{
        public readonly string Port = Port;
        public readonly NetworkPayload? Data = Data;
        public readonly EntityUid? Trigger = Trigger;
}

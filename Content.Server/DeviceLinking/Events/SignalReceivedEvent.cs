using Content.Server.DeviceNetwork;

namespace Content.Server.DeviceLinking.Events
{
    public sealed class SignalReceivedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly NetworkPayload? Data;
        public readonly EntityUid? Trigger;

        public SignalReceivedEvent(string port, EntityUid? trigger = null, NetworkPayload? data = null)
        {
            Port = port;
            Trigger = trigger;
            Data = data;
        }

    }
}

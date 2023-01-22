using Content.Server.DeviceNetwork;

namespace Content.Server.DeviceLinking.Events
{
    public sealed class SignalReceivedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly NetworkPayload? Data;

        public SignalReceivedEvent(string port, NetworkPayload? data = null)
        {
            Port = port;
            Data = data;
        }
    }
}

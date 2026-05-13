namespace Content.Shared.DeviceLinking.Events
{
    public sealed partial class PortDisconnectedEvent : EntityEventArgs
    {
        public readonly string Port;

        public PortDisconnectedEvent(string port)
        {
            Port = port;
        }
    }
}


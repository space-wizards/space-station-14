namespace Content.Shared.DeviceLinking.Events
{
    public sealed class PortDisconnectedEvent : EntityEventArgs
    {
        public readonly string Port;

        public readonly EntityUid Source;

        public readonly EntityUid Sink;

        public PortDisconnectedEvent(string port, EntityUid source, EntityUid sink)
        {
            Port = port;
            Source = source;
            Sink = sink;
        }
    }
}

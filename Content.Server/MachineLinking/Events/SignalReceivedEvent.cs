namespace Content.Server.MachineLinking.Events
{
    public sealed class SignalReceivedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly EntityUid? Trigger;

        public SignalReceivedEvent(string port, EntityUid? trigger)
        {
            Port = port;
            Trigger = trigger;
        }
    }
}

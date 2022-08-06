namespace Content.Server.MachineLinking.Events
{
    public sealed class SignalReceivedEvent : EntityEventArgs
    {
        public readonly string Port;

        public SignalReceivedEvent(string port)
        {
            Port = port;
        }
    }
}

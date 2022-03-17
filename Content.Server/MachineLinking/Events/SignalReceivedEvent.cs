using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Events
{
    public sealed class SignalReceivedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly object? Value;

        public SignalReceivedEvent(string port, object? value)
        {
            Port = port;
            Value = value;
        }
    }
}

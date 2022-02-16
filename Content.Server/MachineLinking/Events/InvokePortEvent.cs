using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Events
{
    public sealed class InvokePortEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly object? Value;

        public InvokePortEvent(string port, object? value = null)
        {
            Port = port;
            Value = value;
        }
    }
}

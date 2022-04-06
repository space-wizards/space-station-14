using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Events
{
    public sealed class InvokePortEvent : EntityEventArgs
    {
        public readonly string Port;

        public InvokePortEvent(string port)
        {
            Port = port;
        }
    }
}

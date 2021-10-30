using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Events
{
    public class PortDisconnectedEvent : EntityEventArgs
    {
        public readonly string Port;

        public PortDisconnectedEvent(string port)
        {
            Port = port;
        }
    }
}

using System;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Events
{
    public sealed class SignalValueRequestedEvent : HandledEntityEventArgs
    {
        public readonly string Port;
        public readonly Type Type;

        public object? Signal;

        public SignalValueRequestedEvent(string port, Type type)
        {
            Port = port;
            Type = type;
        }
    }
}

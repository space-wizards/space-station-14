using Content.Server.DeviceLinking.Components;

namespace Content.Server.MachineLinking.Events
{
    public sealed class SignalReceivedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly SignalState State;
        public readonly EntityUid? Trigger;

        public SignalReceivedEvent(string port, EntityUid? trigger, SignalState state)
        {
            Port = port;
            Trigger = trigger;
            State = state;
        }
    }
}

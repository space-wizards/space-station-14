namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalLinkerComponent : Component
    {
        [ViewVariables]
        public EntityUid? SavedTransmitter;

        [ViewVariables]
        public EntityUid? SavedReceiver;
    }
}

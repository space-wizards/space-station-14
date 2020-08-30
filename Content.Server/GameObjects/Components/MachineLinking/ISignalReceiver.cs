namespace Content.Server.GameObjects.Components.MachineLinking
{
    public interface ISignalReceiver
    {
        void TriggerSignal(SignalState state);
    }

    public enum SignalState
    {
        On,
        Off,
        Toggle
    }
}

namespace Content.Server.MachineLinking.Components
{
    public interface ISignalReceiver<in T>
    {
        void TriggerSignal(T signal);
    }
}

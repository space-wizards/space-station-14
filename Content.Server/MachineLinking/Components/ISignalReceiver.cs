namespace Content.Server.GameObjects.Components.MachineLinking
{
    public interface ISignalReceiver<in T>
    {
        void TriggerSignal(T signal);
    }
}

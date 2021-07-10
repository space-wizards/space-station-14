using Robust.Shared.GameObjects;

namespace Content.Shared.MachineLinking
{
    public class SignalTransmitterPortSelected : BoundUserInterfaceMessage
    {
        public readonly string Port;

        public SignalTransmitterPortSelected(string port)
        {
            Port = port;
        }
    }
}

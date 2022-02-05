using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalLinkerComponent : Component
    {
        [ViewVariables]
        public (SignalTransmitterComponent transmitter, string port)? Port;
    }
}

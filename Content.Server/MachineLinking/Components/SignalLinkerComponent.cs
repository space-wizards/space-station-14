using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalLinkerComponent : Component
    {
        [ViewVariables]
        public (SignalTransmitterComponent transmitter, string port)? Port;
    }
}

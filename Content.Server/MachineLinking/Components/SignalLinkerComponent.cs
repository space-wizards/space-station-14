using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    public class SignalLinkerComponent : Component
    {
        public override string Name => "SignalLinker";

        [ViewVariables]
        public (SignalTransmitterComponent, string)? Port;
    }
}

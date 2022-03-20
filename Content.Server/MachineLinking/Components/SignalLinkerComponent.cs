using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalLinkerComponent : Component
    {
        [ViewVariables]
        public EntityUid? savedTransmitter;

        [ViewVariables]
        public EntityUid? savedReceiver;
    }
}

using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class SignalLinkerComponent : Component
    {
        public override string Name => "SignalLinker";

        [ViewVariables]
        public SignalTransmitterComponent? Link { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            Link = null;
        }
    }
}

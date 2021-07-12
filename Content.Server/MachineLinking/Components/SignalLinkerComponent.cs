using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalLinkerComponent : Component
    {
        public override string Name => "SignalLinker";

        [ViewVariables]
        public SignalTransmitterComponent? Link { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            Link = null;
        }
    }
}

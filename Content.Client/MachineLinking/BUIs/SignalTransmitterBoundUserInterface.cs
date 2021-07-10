using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.MachineLinking.BUIs
{
    public class SignalTransmitterBoundUserInterface : BoundUserInterface
    {
        public SignalTransmitterBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }
    }
}

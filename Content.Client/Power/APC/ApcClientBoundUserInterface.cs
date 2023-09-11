using Content.Client.Power.APC.UI;
using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC
{
    [UsedImplicitly]
    public sealed class ApcClientBoundUserInterface : ClientBoundUserInterface
    {
        [ViewVariables]
        private ApcMenu? _menu;

        public ApcClientBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new ApcMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;
            _menu?.UpdateState(castState);
        }

        public void BreakerPressed()
        {
            SendMessage(new ApcToggleMainBreakerMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _menu?.Dispose();
            }
        }
    }
}

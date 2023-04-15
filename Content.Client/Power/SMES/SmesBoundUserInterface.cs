using Content.Client.Power.SMES.UI;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.Power.SMES
{
    [UsedImplicitly]
    public sealed class SmesBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private SmesMenu? _menu;

        protected override void Open()
        {
            base.Open();

            _menu = new SmesMenu(this, Owner);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public SmesBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not SmesBoundInterfaceState cast || _menu == null)
            {
                return;
            }
            _menu.UpdateState(cast);
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

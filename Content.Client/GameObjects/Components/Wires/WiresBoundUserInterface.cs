using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components.Wires
{
    public class WiresBoundUserInterface : BoundUserInterface
    {
        public WiresBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private WiresMenu _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new WiresMenu(this);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _menu.Populate((WiresBoundUserInterfaceState) state);
        }

        public void PerformAction(int id, WiresAction action)
        {
            SendMessage(new WiresActionMessage(id, action));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu.Dispose();
        }
    }
}

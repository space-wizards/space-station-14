using Content.Shared.Wires;
using Robust.Client.UserInterface;

namespace Content.Client.Wires.UI
{
    public sealed class WiresBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private WiresMenu? _menu;

        public WiresBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<WiresMenu>();
            _menu.OnAction += PerformAction;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _menu?.Populate((WiresBoundUserInterfaceState) state);
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

            _menu?.Dispose();
        }
    }
}

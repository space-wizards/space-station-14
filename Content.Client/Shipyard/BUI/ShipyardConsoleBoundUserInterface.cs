using Content.Client.Shipyard.UI;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Events;
using Robust.Client.GameObjects;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Shipyard.BUI
{
    public sealed class ShipyardConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ShipyardConsoleMenu? _menu;

        [ViewVariables]
        public int Balance { get; private set; }

        public ShipyardConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = new ShipyardConsoleMenu(this);
            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.OnOrderApproved += ApproveOrder;
        }

        private void Populate()
        {
            if (_menu == null)
                return;

            _menu.PopulateProducts();
            _menu.PopulateCategories();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not ShipyardConsoleInterfaceState cState)
                return;

            Balance = cState.Balance;
            var castState = (ShipyardConsoleInterfaceState) state;
            Populate();
            _menu?.UpdateState(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            _menu?.Dispose();
        }

        private void ApproveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not VesselRow row || row.Vessel == null)
            {
                return;
            }

            var vesselId = row.Vessel.ID;
            SendMessage(new ShipyardConsolePurchaseMessage(vesselId));
        }
    }
}

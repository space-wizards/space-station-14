using Content.Client.VendingMachines;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.GameObjects.Components.VendingMachines
{
    class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private VendingMachineMenu? _menu;

        public VendingMachineBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new VendingMachineMenu(this) {Title = Owner.Owner.Name};
            SendMessage(new InventorySyncRequestMessage());

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void Eject(string id)
        {
            SendMessage(new VendingMachineEjectMessage(id));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case VendingMachineInventoryMessage msg:
                    _menu?.Populate(msg.Inventory);
                    break;
            }
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

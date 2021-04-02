using Content.Client.VendingMachines;
using Content.Shared.GameObjects.Components.VendingMachines;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.GameObjects.Components.VendingMachines
{
    class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private VendingMachineMenu? _menu;

        public SharedVendingMachineComponent? VendingMachine { get; private set; }

        public VendingMachineBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new InventorySyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out SharedVendingMachineComponent? vendingMachine))
            {
                return;
            }

            VendingMachine = vendingMachine;

            _menu = new VendingMachineMenu(this) {Title = Owner.Owner.Name};
            _menu.Populate(VendingMachine.Inventory);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void Eject(string ID)
        {
            SendMessage(new VendingMachineEjectMessage(ID));
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

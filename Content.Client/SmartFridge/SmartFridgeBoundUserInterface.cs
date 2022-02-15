using Content.Client.SmartFridge.UI;
using Content.Shared.SmartFridge;
using Robust.Client.GameObjects;
using Robust.Shared.Log;

using static Content.Shared.SmartFridge.SharedSmartFridgeComponent;

namespace Content.Client.SmartFridge
{
    public class SmartFridgeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private SmartFridgeMenu? _menu;

        public SharedSmartFridgeComponent? SmartFridge { get; private set; }

        public SmartFridgeBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new InventorySyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();

            _menu = new SmartFridgeMenu(this) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};
            SendMessage(new InventorySyncRequestMessage());
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void Eject(uint id)
        {
            SendMessage(new SmartFridgeEjectMessage(id, false));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case SmartFridgeInventoryMessage msg:
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

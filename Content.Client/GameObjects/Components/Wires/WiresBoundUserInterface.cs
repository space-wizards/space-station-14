using System;
using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components
{
    public class WiresBoundUserInterface : BoundUserInterface
    {
        private WiresMenu _menu;

        public SharedWiresComponent Wires { get; private set; }

        public WiresBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new WiresSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out SharedWiresComponent wires))
            {
                return;
            }

            Wires = wires;


            _menu = new WiresMenu() {Owner = this};
            //_menu.Populate(Wires.WiresList);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch(message)
            {
                case WiresListMessage msg:
                    _menu.Populate(msg.WiresList);
                    break;
            }
        }

        public void PerformAction(Guid guid, WiresAction action)
        {
            SendMessage(new WiresActionMessage(guid, action));
        }
    }
}

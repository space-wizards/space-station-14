using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.PipeDispenser
{
    public class PipeDispenserBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private PipeDispenserMenu _menu;

        public SharedPipeDispenserComponent PipeDispenser { get; private set; }

        public PipeDispenserBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new SharedPipeDispenserComponent.InventorySyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            if(!Owner.Owner.TryGetComponent(out SharedPipeDispenserComponent PipeDispenser))
            {
                return;
            }

            this.PipeDispenser = PipeDispenser;

            _menu = new PipeDispenserMenu() { Owner = this, Title = Owner.Owner.Name };

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void Eject(string id, uint quantity)
        {
            SendMessage(new SharedPipeDispenserComponent.PipeDispenserEjectMessage(id, quantity));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch(message)
            {
                case SharedPipeDispenserComponent.PipeDispenserInventoryMessage msg:
                    PopulateMenu(msg.Inventory);
                    break;
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(!disposing) { return; }
            _menu?.Dispose();
        }

        private SpriteSpecifier GetPrototypeIcon(EntityPrototype prototype)
        {
            if (prototype.Components.TryGetValue("Icon", out var iconNode))
                return SpriteSpecifier.FromYaml(iconNode);


            return SpriteSpecifier.Invalid;
        }

        /// <summary>
        /// Gets the name and icon from all inventory entries and puts them into the menu
        /// </summary>
        /// <param name="inventory"></param>
        private void PopulateMenu(List<SharedPipeDispenserComponent.PipeDispenserInventoryEntry> inventory)
        {
            var prototypeManger = IoCManager.Resolve<IPrototypeManager>();
            foreach (var entry in inventory)
            {
                if (prototypeManger.TryIndex(entry.ID, out EntityPrototype prototype))
                {
                    var icon = GetPrototypeIcon(prototype);
                    var name = prototype.Name;
                    _menu.AddItem(entry.ID, name, icon);
                }
            }
        }
    }
}

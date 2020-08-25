using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    [UsedImplicitly]
    public class StrippableBoundUserInterface : BoundUserInterface
    {
        public Dictionary<Slots, string> Inventory { get; private set; }
        public Dictionary<string, string> Hands { get; private set; }

        [ViewVariables]
        private StrippingMenu _strippingMenu;

        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _strippingMenu = new StrippingMenu($"{Owner.Owner.Name}'s inventory");

            _strippingMenu.OnClose += Close;
            _strippingMenu.OpenCentered();
            UpdateMenu();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _strippingMenu.Dispose();

            _strippingMenu.Close();
        }

        private void UpdateMenu()
        {
            if (_strippingMenu == null) return;

            _strippingMenu.ClearButtons();

            if(Inventory != null)
                foreach (var (slot, name) in Inventory)
                {
                    _strippingMenu.AddButton(EquipmentSlotDefines.SlotNames[slot], name, (ev) =>
                    {
                        SendMessage(new StrippingInventoryButtonPressed(slot));
                    });
                }

            if(Hands != null)
                foreach (var (hand, name) in Hands)
                {
                    _strippingMenu.AddButton(hand, name, (ev) =>
                    {
                        SendMessage(new StrippingHandButtonPressed(hand));
                    });
                }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (!(state is StrippingBoundUserInterfaceState stripState)) return;

            Inventory = stripState.Inventory;
            Hands = stripState.Hands;

            UpdateMenu();
        }
    }
}

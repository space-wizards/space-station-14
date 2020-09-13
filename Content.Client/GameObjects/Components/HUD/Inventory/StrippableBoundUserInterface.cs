using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    [UsedImplicitly]
    public class StrippableBoundUserInterface : BoundUserInterface
    {
        public Dictionary<Slots, string> Inventory { get; private set; }
        public Dictionary<string, string> Hands { get; private set; }
        public Dictionary<EntityUid, string> Handcuffs { get; private set; }

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

            if (Inventory != null)
            {
                foreach (var (slot, name) in Inventory)
                {
                    _strippingMenu.AddButton(SlotNames[slot], name, (ev) =>
                    {
                        SendMessage(new StrippingInventoryButtonPressed(slot));
                    });
                }
            }

            if (Hands != null)
            {
                foreach (var (hand, name) in Hands)
                {
                    _strippingMenu.AddButton(hand, name, (ev) =>
                    {
                        SendMessage(new StrippingHandButtonPressed(hand));
                    });
                }
            }

            if (Handcuffs != null)
            {
                foreach (var (id, name) in Handcuffs)
                {
                    _strippingMenu.AddButton(Loc.GetString("Restraints"), name, (ev) =>
                    {
                        SendMessage(new StrippingHandcuffButtonPressed(id));
                    });
                }
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (!(state is StrippingBoundUserInterfaceState stripState)) return;

            Inventory = stripState.Inventory;
            Hands = stripState.Hands;
            Handcuffs = stripState.Handcuffs;

            UpdateMenu();
        }
    }
}

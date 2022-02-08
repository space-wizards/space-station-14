using System.Collections.Generic;
using Content.Client.Strip;
using Content.Shared.Strip.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public class StrippableBoundUserInterface : BoundUserInterface
    {
        public Dictionary<(string ID, string Name), string>? Inventory { get; private set; }
        public Dictionary<string, string>? Hands { get; private set; }
        public Dictionary<EntityUid, string>? Handcuffs { get; private set; }

        [ViewVariables]
        private StrippingMenu? _strippingMenu;

        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _strippingMenu = new StrippingMenu($"{Loc.GetString("strippable-bound-user-interface-stripping-menu-title",("ownerName", IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName))}");

            _strippingMenu.OnClose += Close;
            _strippingMenu.OpenCentered();
            UpdateMenu();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _strippingMenu?.Dispose();
        }

        private void UpdateMenu()
        {
            if (_strippingMenu == null) return;

            _strippingMenu.ClearButtons();

            if (Inventory != null)
            {
                foreach (var (slot, name) in Inventory)
                {
                    _strippingMenu.AddButton(slot.Name, name, (ev) =>
                    {
                        SendMessage(new StrippingInventoryButtonPressed(slot.ID));
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
                    _strippingMenu.AddButton(Loc.GetString("strippable-bound-user-interface-stripping-menu-handcuffs-button"), name, (ev) =>
                    {
                        SendMessage(new StrippingHandcuffButtonPressed(id));
                    });
                }
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not StrippingBoundUserInterfaceState stripState) return;

            Inventory = stripState.Inventory;
            Hands = stripState.Hands;
            Handcuffs = stripState.Handcuffs;

            UpdateMenu();
        }
    }
}

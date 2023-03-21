using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Content.Shared.Bank.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        public VendingMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();
            var vendingMachineSys = entMan.System<VendingMachineSystem>();
            var priceMod = 1f;

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner.Owner, out var market))
            {
                priceMod = market.Mod;
            }

            _cachedInventory = vendingMachineSys.GetAllInventory(Owner.Owner);

            _menu = new VendingMachineMenu {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;

            _menu.Populate(_cachedInventory, priceMod);

            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var priceMod = 1f;

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner.Owner, out var market))
            {
                priceMod = market.Mod;
            }
            _cachedInventory = newState.Inventory;
            _menu?.UpdateBalance(newState.Balance);
            _menu?.Populate(_cachedInventory, priceMod);
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(args.ItemIndex);

            if (selectedItem == null)
                return;

            SendMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}

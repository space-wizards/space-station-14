// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.SmartFridge.UI;
using Content.Shared.SS220.SmartFridge;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface.Controls;
using System.Linq;

namespace Content.Client.SS220.SmartFridge
{
    public sealed class SmartFridgeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private SmartFridgeMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        public SmartFridgeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new SmartFridgeMenu { Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName };

            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;
            _menu.OnSearchChanged += OnSearchChanged;

            UpdateUI();

            _menu.OpenCentered();
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(args.ItemIndex));

            if (selectedItem == null)
                return;

            SendPredictedMessage(new SmartFridgeInteractWithItemEvent(selectedItem.EntityUids[0]));

            UpdateUI();
        }

        public void UpdateUI()
        {
            var smartFridgeSys = EntMan.System<SharedSmartFridgeSystem>();
            _cachedInventory = smartFridgeSys.GetAllInventory(Owner);
            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex);
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

        private void OnSearchChanged(string? filter)
        {
            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, filter);
        }
    }

}

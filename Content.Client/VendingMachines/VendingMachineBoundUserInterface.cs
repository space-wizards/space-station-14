using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();
        private bool _showPrices = true;  // 🌟Starlight🌟 Track if prices should be shown

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindowCenteredLeft<VendingMachineMenu>();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            _menu.OnItemSelected += OnItemSelected;
            
            Refresh(); // 🌟Starlight🌟
            
            // 🌟Starlight🌟 
            if (_showPrices)
            {
                RequestBalance(); // Client ask too, server also pushes on open now
            }
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (message is VendingMachineBalanceUpdateMessage balanceMessage)
            {
                _menu?.UpdateBalance(balanceMessage.Balance);
            }
        }

        /// <summary>
        /// Requests current balance from server
        /// </summary>
        private void RequestBalance()         // 🌟Starlight🌟
        {
            SendMessage(new VendingMachineRequestBalanceMessage());
        }

        public void Refresh()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;
            _showPrices = bendy?.ShowPrices ?? true;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory, enabled, _showPrices);

            // 🌟Starlight start🌟 
            if (_menu != null)
            {
                if (_showPrices)
                {
                    _menu.ToggleBalance(true);
                    RequestBalance();
                }
                else
                {
                    _menu.ToggleBalance();
                }
            }
            //  // 🌟Starlight end🌟
        }

        public void UpdateAmounts()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;
            _showPrices = bendy?.ShowPrices ?? true; // 🌟Starlight🌟

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);
            _menu?.UpdateAmounts(_cachedInventory, enabled, _showPrices); // 🌟Starlight🌟

            // 🌟Starlight start🌟 
            if (_menu != null)
            {
                if (_showPrices)
                {
                    _menu.ToggleBalance(true);
                    RequestBalance();
                }
                else
                {
                    _menu.ToggleBalance();
                }
            }
             // 🌟Starlight end🌟 
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

            if (selectedItem == null)
                return;

            // 🌟Starlight🌟 Use non-predicted message so that the server processes the vend
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

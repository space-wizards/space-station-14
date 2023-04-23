using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.GameObjects;
using System.Linq;
using Robust.Shared.Map;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables]
        private VendingMachineMenu? _menu;

        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        public VendingMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            var vendingMachineSys = _entityManager.System<VendingMachineSystem>();

            _cachedInventory = vendingMachineSys.GetAllInventory(Owner.Owner);

            var meta = _entityManager.GetComponent<MetaDataComponent>(Owner.Owner);

            _menu = new VendingMachineMenu(meta.EntityPrototype!.ID)
            {
                Title = meta.EntityName,
            };

            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;

            _menu.Populate(_cachedInventory);

            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory;

            _menu?.Populate(_cachedInventory);
        }

        private void OnItemSelected(int index)
        {
            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(index);
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

            _entityManager.DeleteEntity(_menu.ent);
            foreach (var entity in _menu.ents) _entityManager.DeleteEntity(entity);

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}

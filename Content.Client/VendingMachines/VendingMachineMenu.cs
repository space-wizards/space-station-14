using System.Collections.Generic;
using Content.Client.GameObjects.Components.VendingMachines;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.VendingMachines
{
    class VendingMachineMenu : SS14Window
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        protected override Vector2? CustomSize => (300, 450);

        private readonly ItemList _items;
        private List<VendingMachineInventoryEntry> _cachedInventory;

        public VendingMachineBoundUserInterface Owner { get; set; }

        public VendingMachineMenu()
        {
            IoCManager.InjectDependencies(this);

            _items = new ItemList()
            {
                SizeFlagsStretchRatio = 8,
                SizeFlagsVertical = SizeFlags.FillExpand,
            };
            _items.OnItemSelected += ItemSelected;

            Contents.AddChild(_items);
        }

        public void Populate(List<VendingMachineInventoryEntry> inventory)
        {
            _items.Clear();
            _cachedInventory = inventory;
            foreach (VendingMachineInventoryEntry entry in inventory)
            {
                var itemName = _prototypeManager.Index<EntityPrototype>(entry.ID).Name;

                Texture icon = null;
                if(_prototypeManager.TryIndex(entry.ID, out EntityPrototype prototype))
                {
                    icon = SpriteComponent.GetPrototypeIcon(prototype, _resourceCache)?.Default;
                }
                _items.AddItem($"{itemName} ({entry.Amount} left)", icon);
            }
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            Owner.Eject(_cachedInventory[args.ItemIndex].ID);
        }
    }
}

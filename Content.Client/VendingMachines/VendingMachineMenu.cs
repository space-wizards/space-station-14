using Content.Client.GameObjects.Components.VendingMachines;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.VendingMachines
{
    class VendingMachineMenu : SS14Window
    {
        protected override Vector2? CustomSize => (300, 450);

        private ItemList _items;
        private List<VendingMachineInventoryEntry> _cachedInventory;

        #pragma warning disable CS0649
        [Dependency]
        private IResourceCache _resourceCache;
        [Dependency]
        private readonly IPrototypeManager _prototypeManager;
        #pragma warning restore
        public VendingMachineBoundUserInterface Owner { get; set; }
        public VendingMachineMenu()
        {
        }

        public VendingMachineMenu(string name) : base(name)
        {

        }

        protected override void Initialize()
        {
            base.Initialize();
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
                Texture icon = null;
                if(_prototypeManager.TryIndex(entry.ID, out EntityPrototype prototype))
                {
                    icon = IconComponent.GetPrototypeIcon(prototype, _resourceCache).TextureFor(Direction.South);
                }
                _items.AddItem($"{entry.ID} ({entry.Amount} left)", icon);
            }
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            Owner.Eject(_cachedInventory[args.ItemIndex].ID);
        }
    }
}

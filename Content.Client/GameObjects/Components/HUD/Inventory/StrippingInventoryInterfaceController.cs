using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Client.Utility;
using JetBrains.Annotations;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    // Dynamically instantiated by ClientInventoryComponent.
    [UsedImplicitly]
    public class StrippingInventoryInterfaceController : InventoryInterfaceController
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        private readonly Dictionary<Slots, List<ItemSlotButton>> _inventoryButtons
            = new Dictionary<Slots, List<ItemSlotButton>>();

        private Control _quickButtonsContainer;

        public StrippingInventoryInterfaceController(ClientInventoryComponent owner) : base(owner)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            _window = new StrippingInventoryWindow(_loc, _resourceCache);

            // behavior of the buttons i'll have to change eventually with SBUI code.
            // gotta figure out how to summon the screen in the first place.
            foreach (var (slot, button) in _window.Buttons)
            {
//               button.OnPressed = (e) => SendMessage(new StrippingInventoryButtonPressed(slot));
  //             button.OnHover = (e) => RequestItemHover(slot);
    //           _inventoryButtons.Add(slot, new List<ItemSlotButton> { button });
            }
        }

        public override SS14Window Window => _window;
        private StrippingInventoryWindow _window;

        public override void AddToSlot(Slots slot, IEntity entity)
        {
            base.AddToSlot(slot, entity);

            if (!_inventoryButtons.TryGetValue(slot, out var buttons))
                return;

            foreach (var button in buttons)
            {
                _itemSlotManager.SetItemSlot(button, entity);
                button.OnPressed = (e) => HandleInventoryKeybind(e, slot);
            }
        }

        public override void RemoveFromSlot(Slots slot)
        {
            base.RemoveFromSlot(slot);

            if (!_inventoryButtons.TryGetValue(slot, out var buttons))
            {
                return;
            }

            foreach (var button in buttons)
            {
                ClearButton(button, slot);
            }
        }

        public override void HoverInSlot(Slots slot, IEntity entity, bool fits)
        {
            base.HoverInSlot(slot, entity, fits);

            if (!_inventoryButtons.TryGetValue(slot, out var buttons))
            {
                return;
            }

            foreach (var button in buttons)
            {
                _itemSlotManager.HoverInSlot(button, entity, fits);
            }
        }

        protected override void HandleInventoryKeybind(GUIBoundKeyEventArgs args, Slots slot)
        {
            if (!_inventoryButtons.TryGetValue(slot, out var buttons))
                return;
            if (!Owner.TryGetSlot(slot, out var item))
                return;
            if (_itemSlotManager.OnButtonPressed(args, item))
                return;

            base.HandleInventoryKeybind(args, slot);
        }
        // this relies on inventoryinterfacecontroller which i kinda want to stay clear from.


        private void ClearButton(ItemSlotButton button, Slots slot)
        {
            button.OnPressed = (e) => AddToInventory(e, slot);
            _itemSlotManager.SetItemSlot(button, null);
        }

        public override void PlayerAttached()
        {
            base.PlayerAttached();

            GameHud.InventoryQuickButtonContainer.AddChild(_quickButtonsContainer);

            // Update all the buttons to make sure they check out.

            foreach (var (slot, buttons) in _inventoryButtons)
            {
                foreach (var button in buttons)
                {
                    ClearButton(button, slot);
                }

                if (Owner.TryGetSlot(slot, out var entity))
                {
                    AddToSlot(slot, entity);
                }
            }
        }

        public override void PlayerDetached()
        {
            base.PlayerDetached();

            GameHud.InventoryQuickButtonContainer.RemoveChild(_quickButtonsContainer);

            foreach (var (slot, list) in _inventoryButtons)
            {
                foreach (var button in list)
                {
                    ClearButton(button, slot);
                }
            }
        }

        private class StrippingInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 2;
            private const int RightSeparation = 2;

            public IReadOnlyDictionary<Slots, ItemSlotButton> Buttons { get; }

            public StrippingInventoryWindow(ILocalizationManager loc, IResourceCache resourceCache)
            {
                Title = loc.GetString("Your Inventory");
                Resizable = false;

                var buttonDict = new Dictionary<Slots, ItemSlotButton>();
                Buttons = buttonDict;

                const int width = ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation;
                const int height = ButtonSize * 4 + ButtonSeparation * 3;

                var windowContents = new LayoutContainer {CustomMinimumSize = (width, height)};
                Contents.AddChild(windowContents);

                void AddButton(Slots slot, string textureName, Vector2 position)
                {
                    var texture = resourceCache.GetTexture($"/Textures/Interface/Inventory/{textureName}.png");
                    var storageTexture = resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
                    var button = new ItemSlotButton(texture, storageTexture);

                    LayoutContainer.SetPosition(button, position);

                    windowContents.AddChild(button);
                    buttonDict.Add(slot, button);
                }

                const int sizep = (ButtonSize + ButtonSeparation);

                // Left column.
                AddButton(Slots.EYES, "glasses", (0, 0));
                AddButton(Slots.NECK, "neck", (0, sizep));
                AddButton(Slots.INNERCLOTHING, "uniform", (0, 2 * sizep));

                // Middle column.
                AddButton(Slots.HEAD, "head", (sizep, 0));
                AddButton(Slots.MASK, "mask", (sizep, sizep));
                AddButton(Slots.OUTERCLOTHING, "suit", (sizep, 2 * sizep));
                AddButton(Slots.SHOES, "shoes", (sizep, 3 * sizep));

                // Right column
                AddButton(Slots.EARS, "ears", (2 * sizep, 0));
                AddButton(Slots.IDCARD, "id", (2 * sizep, sizep));
                AddButton(Slots.EXOSUITSLOT1, "suit_storage", (2 * sizep, 2 * sizep));
                AddButton(Slots.POCKET1, "pocket", (2 * sizep, 3 * sizep));

                // Far right column.
                AddButton(Slots.BACKPACK, "back", (3 * sizep, 0));
                AddButton(Slots.BELT, "belt", (3 * sizep, sizep));
                AddButton(Slots.GLOVES, "gloves", (3 * sizep, 2 * sizep));
                AddButton(Slots.POCKET2, "pocket", (3 * sizep, 3 * sizep));
            }
        }
    }
}


// no longer planning on using this file outside of reference material i think. Issues with owner classes.

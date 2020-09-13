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
    public class HumanInventoryInterfaceController : InventoryInterfaceController
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        private readonly Dictionary<Slots, List<ItemSlotButton>> _inventoryButtons
            = new Dictionary<Slots, List<ItemSlotButton>>();

        private ItemSlotButton _hudButtonPocket1;
        private ItemSlotButton _hudButtonPocket2;
        private ItemSlotButton _hudButtonBelt;
        private ItemSlotButton _hudButtonBack;
        private ItemSlotButton _hudButtonId;
        private Control _quickButtonsContainer;

        public HumanInventoryInterfaceController(ClientInventoryComponent owner) : base(owner)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            _window = new HumanInventoryWindow(_loc, _resourceCache);
            _window.OnClose += () => GameHud.InventoryButtonDown = false;
            foreach (var (slot, button) in _window.Buttons)
            {
                button.OnPressed = (e) => AddToInventory(e, slot);
                button.OnStoragePressed = (e) => OpenStorage(e, slot);
                button.OnHover = (e) => RequestItemHover(slot);
                _inventoryButtons.Add(slot, new List<ItemSlotButton> {button});
            }

            void AddButton(out ItemSlotButton variable, Slots slot, string textureName)
            {
                var texture = _resourceCache.GetTexture($"/Textures/Interface/Inventory/{textureName}.png");
                var storageTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
                variable = new ItemSlotButton(texture, storageTexture)
                {
                    OnPressed = (e) => AddToInventory(e, slot),
                    OnStoragePressed = (e) => OpenStorage(e, slot),
                    OnHover = (e) => RequestItemHover(slot)
                };
                _inventoryButtons[slot].Add(variable);
            }

            AddButton(out _hudButtonPocket1, Slots.POCKET1, "pocket");
            AddButton(out _hudButtonPocket2, Slots.POCKET2, "pocket");
            AddButton(out _hudButtonBack, Slots.BACKPACK, "back");
            AddButton(out _hudButtonBelt, Slots.BELT, "belt");
            AddButton(out _hudButtonId, Slots.IDCARD, "id");

            _quickButtonsContainer = new HBoxContainer
            {
                Children =
                {
                    _hudButtonId,
                    _hudButtonBelt,
                    _hudButtonBack,
                    _hudButtonPocket1,
                    _hudButtonPocket2,
                }
            };
        }

        public override SS14Window Window => _window;
        private HumanInventoryWindow _window;

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

        private class HumanInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 2;
            private const int RightSeparation = 2;

            public IReadOnlyDictionary<Slots, ItemSlotButton> Buttons { get; }

            public HumanInventoryWindow(ILocalizationManager loc, IResourceCache resourceCache)
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
                AddButton(Slots.POCKET1, "pocket", (0, 3 * sizep));

                // Middle column.
                AddButton(Slots.HEAD, "head", (sizep, 0));
                AddButton(Slots.MASK, "mask", (sizep, sizep));
                AddButton(Slots.OUTERCLOTHING, "suit", (sizep, 2 * sizep));
                AddButton(Slots.SHOES, "shoes", (sizep, 3 * sizep));

                // Right column
                AddButton(Slots.EARS, "ears", (2 * sizep, 0));
                AddButton(Slots.IDCARD, "id", (2 * sizep, sizep));
                AddButton(Slots.GLOVES, "gloves", (2 * sizep, 2 * sizep));
                AddButton(Slots.POCKET2, "pocket", (2 * sizep, 3 * sizep));

                // Far right column.
                AddButton(Slots.BACKPACK, "back", (3 * sizep, 0));
                AddButton(Slots.BELT, "belt", (3 * sizep, sizep));
            }
        }
    }
}

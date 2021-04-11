using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared;
using Content.Shared.Prototypes.HUD;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    // Dynamically instantiated by ClientInventoryComponent.
    [UsedImplicitly]
    public class HumanInventoryInterfaceController : InventoryInterfaceController
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<Slots, List<ItemSlotButton>> _inventoryButtons
            = new();

        private ItemSlotButton _hudButtonPocket1 = default!;
        private ItemSlotButton _hudButtonPocket2 = default!;
        private ItemSlotButton _hudButtonShoes = default!;
        private ItemSlotButton _hudButtonJumpsuit = default!;
        private ItemSlotButton _hudButtonGloves = default!;
        private ItemSlotButton _hudButtonNeck = default!;
        private ItemSlotButton _hudButtonHead = default!;
        private ItemSlotButton _hudButtonBelt = default!;
        private ItemSlotButton _hudButtonBack = default!;
        private ItemSlotButton _hudButtonOClothing = default!;
        private ItemSlotButton _hudButtonId = default!;
        private ItemSlotButton _hudButtonMask = default!;
        private ItemSlotButton _hudButtonEyes = default!;
        private ItemSlotButton _hudButtonEars = default!;

        private Control _topQuickButtonsContainer = default!;
        private Control _bottomLeftQuickButtonsContainer = default!;
        private Control _bottomRightQuickButtonsContainer = default!;

        public HumanInventoryInterfaceController(ClientInventoryComponent owner) : base(owner)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            _configManager.OnValueChanged(CCVars.HudTheme, UpdateHudTheme, invokeImmediately: true);

            _window = new HumanInventoryWindow(_gameHud);
            _window.OnClose += () => GameHud.InventoryButtonDown = false;
            foreach (var (slot, button) in _window.Buttons)
            {
                button.OnPressed = (e) => AddToInventory(e, slot);
                button.OnStoragePressed = (e) => OpenStorage(e, slot);
                button.OnHover = (_) => RequestItemHover(slot);
                _inventoryButtons.Add(slot, new List<ItemSlotButton> {button});
            }

            void AddButton(out ItemSlotButton variable, Slots slot, string textureName)
            {
                var texture = _gameHud.GetHudTexture($"{textureName}.png");
                var storageTexture = _gameHud.GetHudTexture("back.png");
                variable = new ItemSlotButton(texture, storageTexture, textureName)
                {
                    OnPressed = (e) => AddToInventory(e, slot),
                    OnStoragePressed = (e) => OpenStorage(e, slot),
                    OnHover = (_) => RequestItemHover(slot)
                };
                _inventoryButtons[slot].Add(variable);
            }

            AddButton(out _hudButtonPocket1, Slots.POCKET1, "pocket");
            AddButton(out _hudButtonPocket2, Slots.POCKET2, "pocket");
            AddButton(out _hudButtonId, Slots.IDCARD, "id");

            AddButton(out _hudButtonBack, Slots.BACKPACK, "back");

            AddButton(out _hudButtonBelt, Slots.BELT, "belt");

            AddButton(out _hudButtonShoes, Slots.SHOES, "shoes");
            AddButton(out _hudButtonJumpsuit, Slots.INNERCLOTHING, "uniform");
            AddButton(out _hudButtonOClothing, Slots.OUTERCLOTHING, "suit");
            AddButton(out _hudButtonGloves, Slots.GLOVES, "gloves");
            AddButton(out _hudButtonNeck, Slots.NECK, "neck");
            AddButton(out _hudButtonMask, Slots.MASK, "mask");
            AddButton(out _hudButtonEyes, Slots.EYES, "glasses");
            AddButton(out _hudButtonEars, Slots.EARS, "ears");
            AddButton(out _hudButtonHead, Slots.HEAD, "head");

            _topQuickButtonsContainer = new HBoxContainer
            {
                Children =
                {
                    _hudButtonShoes,
                    _hudButtonJumpsuit,
                    _hudButtonOClothing,
                    _hudButtonGloves,
                    _hudButtonNeck,
                    _hudButtonMask,
                    _hudButtonEyes,
                    _hudButtonEars,
                    _hudButtonHead
                },
                SeparationOverride = 5
            };

            _bottomRightQuickButtonsContainer = new HBoxContainer
            {
                Children =
                {
                    _hudButtonPocket1,
                    _hudButtonPocket2,
                    _hudButtonId,
                },
                SeparationOverride = 5
            };
            _bottomLeftQuickButtonsContainer = new HBoxContainer
            {
                Children =
                {
                    _hudButtonBelt,
                    _hudButtonBack
                },
                SeparationOverride = 5
            };
        }

        public override SS14Window? Window => _window;
        private HumanInventoryWindow? _window;

        public override IEnumerable<ItemSlotButton> GetItemSlotButtons(Slots slot)
        {
            if (!_inventoryButtons.TryGetValue(slot, out var buttons))
            {
                return Enumerable.Empty<ItemSlotButton>();
            }

            return buttons;
        }

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
            if (!_inventoryButtons.ContainsKey(slot))
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

            GameHud.BottomLeftInventoryQuickButtonContainer.AddChild(_bottomLeftQuickButtonsContainer);
            GameHud.BottomRightInventoryQuickButtonContainer.AddChild(_bottomRightQuickButtonsContainer);
            GameHud.TopInventoryQuickButtonContainer.AddChild(_topQuickButtonsContainer);

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

            GameHud.BottomRightInventoryQuickButtonContainer.RemoveChild(_bottomRightQuickButtonsContainer);
            GameHud.BottomLeftInventoryQuickButtonContainer.RemoveChild(_bottomLeftQuickButtonsContainer);
            GameHud.TopInventoryQuickButtonContainer.RemoveChild(_topQuickButtonsContainer);

            foreach (var (slot, list) in _inventoryButtons)
            {
                foreach (var button in list)
                {
                    ClearButton(button, slot);
                }
            }
        }

        public void UpdateHudTheme(int idx)
        {
            if (!_gameHud.ValidateHudTheme(idx))
            {
                return;
            }

            foreach (var (_, list) in _inventoryButtons)
            {
                foreach (var button in list)
                {
                    button.Button.Texture = _gameHud.GetHudTexture($"{button.TextureName}.png");
                    button.StorageButton.TextureNormal = _gameHud.GetHudTexture("back.png");
                }
            }
        }

        private class HumanInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 4;
            private const int RightSeparation = 2;

            public IReadOnlyDictionary<Slots, ItemSlotButton> Buttons { get; }
            [Dependency] private readonly IGameHud _gameHud = default!;

            public HumanInventoryWindow(IGameHud gameHud)
            {
                Title = Loc.GetString("Your Inventory");
                Resizable = false;

                var buttonDict = new Dictionary<Slots, ItemSlotButton>();
                Buttons = buttonDict;

                const int width = ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation;
                const int height = ButtonSize * 4 + ButtonSeparation * 3;

                var windowContents = new LayoutContainer {MinSize = (width, height)};
                Contents.AddChild(windowContents);

                void AddButton(Slots slot, string textureName, Vector2 position)
                {
                    var texture = gameHud.GetHudTexture($"{textureName}.png");
                    var storageTexture = gameHud.GetHudTexture("back.png");
                    var button = new ItemSlotButton(texture, storageTexture, textureName);

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

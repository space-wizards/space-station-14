using System.Collections.Generic;
using System.Linq;
using Content.Client.HUD;
using Content.Client.Items.Managers;
using Content.Client.Items.UI;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.Inventory.EquipmentSlotDefines;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Inventory
{
    // Dynamically instantiated by ClientInventoryComponent.
    [UsedImplicitly]
    public class HumanInventoryInterfaceController : InventoryInterfaceController
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;

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

        public override void AddToSlot(Slots slot, EntityUid entity)
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

        public override void HoverInSlot(Slots slot, EntityUid entity, bool fits)
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

            if (!_itemSlotManager.OnButtonPressed(args, item))
                base.HandleInventoryKeybind(args, slot);
        }

        private void ClearButton(ItemSlotButton button, Slots slot)
        {
            button.OnPressed = (e) => AddToInventory(e, slot);
            _itemSlotManager.SetItemSlot(button, default);
        }

        private class HumanInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 4;
            private const int RightSeparation = 2;

            public IReadOnlyDictionary<Slots, ItemSlotButton> Buttons { get; }

            public HumanInventoryWindow(IGameHud gameHud)
            {
                Title = Loc.GetString("human-inventory-window-title");
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

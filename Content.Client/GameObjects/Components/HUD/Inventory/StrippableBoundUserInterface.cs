using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.GUI;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

using Content.Client.Utility;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Input;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    [UsedImplicitly]
    public class StrippableBoundUserInterface : BoundUserInterface
    {
        public Dictionary<Slots, EntityUid> Inventory { get; private set; }
        public Dictionary<string, EntityUid> Hands { get; private set; }
        private StrippingInventoryWindow _stripUI;

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
                
        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _stripUI = new StrippingInventoryWindow(_resourceCache, $"{Owner.Owner.Name}'s Inventory");
            _stripUI.OnClose += Close;
            _stripUI.OpenToLeft();

            var entityDict = new Dictionary<Slots, IEntity>();

            foreach (var (slot, button) in _stripUI.Buttons)
            {
                if (button != null)
                {
                    if (slot != Slots.LHAND && slot != Slots.RHAND)
                    {
                        button.OnPressed += args => {
                                if (args.Function == EngineKeyFunctions.Use)
                                    SendMessage(new StrippingInventoryButtonPressed(slot)); };
                    }
                    else
                    {
                        var whichhand = "";
                        if (slot == Slots.LHAND)
                            whichhand = "left hand";
                        if (slot == Slots.RHAND)
                            whichhand = "right hand";
                        
                        button.OnPressed += args => {
                            if (args.Function == EngineKeyFunctions.Use)
                                SendMessage(new StrippingHandButtonPressed(whichhand)); };
                    }
                }
            }
        }

        // okay future incoming problems. special snowflakes indian gods with 16 hands. 

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _stripUI.Dispose();
            _stripUI.Close();
            return;
        }

        public void AddToSlot(Slots slot, EntityUid uid)
        {
            if (!_stripUI.Buttons.TryGetValue(slot, out var button))
                return;

            var entity = IoCManager.Resolve<IEntityManager>().GetEntity(uid);
            _itemSlotManager.SetItemSlot(button, entity);
        }
       
        public void ClearSlots()
        {
            foreach (var (slot, button) in _stripUI.Buttons)
            {
                _itemSlotManager.SetItemSlot(button, null);
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is StrippingBoundUserInterfaceState stripState)) return;
            Inventory = stripState.Inventory;
            Hands = stripState.Hands;

            ClearSlots();

            foreach (var (hand, hold) in Hands)
            {
                if (hand == "left hand")
                {
                    AddToSlot(Slots.LHAND, hold);
                }
                else if (hand == "right hand")
                {
                    AddToSlot(Slots.RHAND, hold);
                }
            }

            foreach (var (slot, uid) in Inventory)
            {
                AddToSlot(slot, uid);
            }
        }

        private class StrippingInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 2;
            private const int RightSeparation = 2;

            public Dictionary<Slots, ItemSlotButton> Buttons { get; }

            public StrippingInventoryWindow(IResourceCache resourceCache, string title)
            {
                Title = title;
                Resizable = false;

                var buttonDict = new Dictionary<Slots, ItemSlotButton>();
                Buttons = buttonDict;

                const int width = ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation;
                const int height = ButtonSize * 4 + ButtonSeparation * 3;
                const int sizep = (ButtonSize + ButtonSeparation);

                var windowContents = new LayoutContainer { CustomMinimumSize = (width, height) };
                Contents.AddChild(windowContents);

                void AddButton(Slots slot, string textureName, Vector2 position)
                {
                    var texture = resourceCache.GetTexture($"/Textures/Interface/Inventory/{textureName}.png");
                    var button = new ItemSlotButton(texture, texture);

                    position *= sizep;
                    LayoutContainer.SetPosition(button, position);
                    windowContents.AddChild(button);

                    buttonDict.Add(slot, button);
                }

                // 0,0  top left.
                // x,x  bottom right.
                // so i don't have hands tied to slots in a consistent smart way quite yet.

                // Left column
                AddButton(Slots.EYES, "glasses", (0, 0));
                AddButton(Slots.NECK, "neck", (0, 1));
                AddButton(Slots.INNERCLOTHING, "uniform", (0, 2));
                AddButton(Slots.BACKPACK, "back", (0, 3));

                // Middle column
                AddButton(Slots.HEAD, "head", (1, 0));
                AddButton(Slots.MASK, "mask", (1, 1));
                AddButton(Slots.OUTERCLOTHING, "suit", (1, 2));
                AddButton(Slots.SHOES, "shoes", (1, 3));

                // Right column
                AddButton(Slots.EARS, "ears", (2, 0));
                AddButton(Slots.GLOVES, "gloves", (2, 1));
                AddButton(Slots.RHAND, "hand_r_no_letter", (2, 2));
                AddButton(Slots.POCKET2, "pocket", (2, 3));

                // Far right column
                AddButton(Slots.IDCARD, "id", (3, 0));
                AddButton(Slots.BELT, "belt", (3, 1));
                AddButton(Slots.LHAND, "hand_l_no_letter", (3, 2));
                AddButton(Slots.POCKET1, "pocket", (3, 3));
            }
        }
    }
}

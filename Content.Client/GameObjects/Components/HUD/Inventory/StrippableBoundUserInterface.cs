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
using Robust.Shared.Log;
using Namotion.Reflection;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    [UsedImplicitly]
    public class StrippableBoundUserInterface : BoundUserInterface
    {
        public Dictionary<Slots, string> Inventory { get; private set; }
        public Dictionary<string, string> Hands { get; private set; }
        public Dictionary<EntityUid, string> Handcuffs { get; private set; }


        // [ViewVariables]
        private StrippingInventoryWindow _stripMenu;

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        // the crossover that was never meant to be.

        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private const string LoggerName = "Storage";


        protected override void Open()
        {
            base.Open();

            _stripMenu = new StrippingInventoryWindow(_resourceCache);
            _stripMenu.OnClose += Close;
            _stripMenu.OpenToLeft();

            var entityDict = new Dictionary<Slots, IEntity>();

            foreach (var (slot, button) in _stripMenu.Buttons)
            {
                if (button != null)
                {
                    Logger.DebugS(LoggerName, $"The {(slot, button)} button has been created.");
                    button.OnPressed = (e) => SendMessage(new StrippingInventoryButtonPressed(slot));
                    // UPDATE: Middleclicks only call OnPressed once. Left/Rights do em twice.
                    // manually trying to overstuff a body sometimes causes redbars to popup everytime you move. investicate later.
                    // overstuffing gets a "you can't drop that." investigate why it's dropping in the first place?
                }
                // I'm not going to try to tackle on hands or cuffs yet. One step at at time.
            }
        }

        // old messages to keep in mind.
        // SendMessage(new StrippingHandcuffButtonPressed(id));
        // SendMessage(new StrippingHandButtonPressed(hand));

        // Generic function for closing the UI. 
        protected override void Dispose(bool disposing)
        {
            Logger.DebugS(LoggerName, $"Dispose called.");
            base.Dispose(disposing);
            if (!disposing) return;
            _stripMenu.Dispose();
            _stripMenu.Close();
            return;
        }

        public void AddToSlot(Slots slot, IEntity entity)
        {
            if (!_stripMenu.Buttons.TryGetValue(slot, out var button))
                return;

            _itemSlotManager.SetItemSlot(button, entity);
        }
        //  i need to find where to get the entity to feed into ientity.
        //  end goal function to fill in the inventory sprites.

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            Logger.DebugS(LoggerName, $"Update state called.");
            // old stuff won't touch.
            base.UpdateState(state);
            if (!(state is StrippingBoundUserInterfaceState stripState)) return;
            Inventory = stripState.Inventory;
            Hands = stripState.Hands;
            Handcuffs = stripState.Handcuffs;
            // old stuff not touching yet.




        }

        // Okay. So I have slots, buttons. I need to have some way to convert a slot -> entity.
        // That way I can sprite it with itemslotmanager. At least that's what I'm seeing.

        private class StrippingInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 2;
            private const int RightSeparation = 2;

            public Dictionary<Slots, ItemSlotButton> Buttons { get; }

            public StrippingInventoryWindow(IResourceCache resourceCache)
            {
                Logger.DebugS(LoggerName, $"StrippingInventoryWindow called.");
                Title = "strip window.";
                Resizable = false;

                var buttonDict = new Dictionary<Slots, ItemSlotButton>();
                Buttons = buttonDict;

                const int width = ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation;
                const int height = ButtonSize * 5 + ButtonSeparation * 3;
                const int sizep = (ButtonSize + ButtonSeparation);

                var windowContents = new LayoutContainer { CustomMinimumSize = (width, height) };
                Contents.AddChild(windowContents);

                void AddButton(Slots slot, string textureName, Vector2 position)
                {
                    var texture = resourceCache.GetTexture($"/Textures/Interface/Inventory/{textureName}.png");
                    //why is storagetexture a thing? only the first input seems to matter.
                    //var storageTexture = resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
                    var button = new ItemSlotButton(texture, texture);

                    position = position * sizep;
                    LayoutContainer.SetPosition(button, position);
                    windowContents.AddChild(button);

                    // took this out, but then it didn't withdraw anything.
                    buttonDict.Add(slot, button);
                    // Logger.DebugS(LoggerName, $"dictionary holds {(slot,button)}");

                    


                }

                // 0,0  top left.
                // x,x  bottom right.
                // still needs slots for hands, handcuffs? not sure how handcuffs going to work here.

                AddButton(Slots.EYES, "glasses", (0, 0));
                AddButton(Slots.NECK, "neck", (0, 1));
                AddButton(Slots.INNERCLOTHING, "uniform", (0, 2));

                AddButton(Slots.HEAD, "head", (1, 0));
                AddButton(Slots.MASK, "mask", (1, 1));
                AddButton(Slots.OUTERCLOTHING, "suit", (1, 2));
                AddButton(Slots.SHOES, "shoes", (1, 3));

                AddButton(Slots.EARS, "ears", (2, 0));
                AddButton(Slots.IDCARD, "id", (2, 1));
                AddButton(Slots.EXOSUITSLOT1, "suit_storage", (2, 2));
                AddButton(Slots.POCKET1, "pocket", (2, 3));
                AddButton(Slots.RHAND, "hand_r_no_letter", (2, 4));

                AddButton(Slots.BACKPACK, "back", (3, 0));
                AddButton(Slots.BELT, "belt", (3, 1));
                AddButton(Slots.GLOVES, "gloves", (3, 2));
                AddButton(Slots.POCKET2, "pocket", (3, 3));
                AddButton(Slots.LHAND, "hand_l_no_letter", (3, 4));
            }
        }
    }
}

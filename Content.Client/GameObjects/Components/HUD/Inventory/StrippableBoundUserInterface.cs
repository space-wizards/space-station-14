using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

using Content.Client.Utility;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

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


        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _stripMenu = new StrippingInventoryWindow(_resourceCache);
            _stripMenu.OnClose += Close;
            _stripMenu.OpenToLeft();


            //_strippingMenu.OnClose += Close;
            //_strippingMenu.OpenCentered();

            UpdateMenu();
        }

        protected override void Dispose(bool disposing)
        {
            //base.Dispose(disposing);
            //if (!disposing) return;
            //_strippingMenu.Dispose();

            //_strippingMenu.Close();

            return;
        }

        private void UpdateMenu()
        {
            if (_stripMenu == null) return;

            //_strippingMenu.ClearButtons();

            //if (Inventory != null)
            //{
            //    foreach (var (slot, name) in Inventory)
            //    {
            //        _strippingMenu.AddButton(EquipmentSlotDefines.SlotNames[slot], name, (ev) =>
            //        {
            //            SendMessage(new StrippingInventoryButtonPressed(slot));
            //        });
            //    }
            //}

            //if (Hands != null)
            //{
            //    foreach (var (hand, name) in Hands)
            //    {
            //        _strippingMenu.AddButton(hand, name, (ev) =>
            //        {
            //            SendMessage(new StrippingHandButtonPressed(hand));
            //        });
            //    }
            //}

            //if (Handcuffs != null)
            //{
            //    foreach (var (id, name) in Handcuffs)
            //    {
            //        _strippingMenu.AddButton(Loc.GetString("Restraints"), name, (ev) =>
            //        {
            //            SendMessage(new StrippingHandcuffButtonPressed(id));
            //        });
            //    }
            //}

            // here is where you rebuild all of the buttons, icons, and interactions. i think.

            return;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (!(state is StrippingBoundUserInterfaceState stripState)) return;

            Inventory = stripState.Inventory;
            Hands = stripState.Hands;
            Handcuffs = stripState.Handcuffs;

            UpdateMenu();
        }


        private class StrippingInventoryWindow : SS14Window
        {
            private const int ButtonSize = 64;
            private const int ButtonSeparation = 2;
            private const int RightSeparation = 2;

            public IReadOnlyDictionary<Slots, ItemSlotButton> Buttons { get; }

            public StrippingInventoryWindow(IResourceCache resourceCache)
            {
                Title = "you're a dirty corpse looter.";
                Resizable = false;

                var buttonDict = new Dictionary<Slots, ItemSlotButton>();
                Buttons = buttonDict;

                const int width = ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation;
                const int height = ButtonSize * 4 + ButtonSeparation * 3;

                var windowContents = new LayoutContainer { CustomMinimumSize = (width, height) };
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

using Content.Shared.GameObjects;
using SS14.Client.GameObjects;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using static Content.Shared.GameObjects.SharedInventoryComponent.ClientInventoryMessage;
using static Content.Shared.GameObjects.SharedInventoryComponent.ServerInventoryMessage;

namespace Content.Client.GameObjects
{
    public class ClientInventoryComponent : SharedInventoryComponent
    {
        private InventoryWindow Window;
        private string TemplateName = "HumanInventory"; //stored for serialization purposes
        public event EventHandler<BoundKeyChangedMessage> OnCharacterMenuKey;

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            Window = new InventoryWindow(this);
            serializer.DataField(ref TemplateName, "Template", "HumanInventory");
            Window.CreateInventory(TemplateName);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                //Updates what we are storing in UI slots
                case ServerInventoryMessage msg:
                    if (msg.Updatetype == ServerInventoryUpdate.Addition)
                    {
                        Window.AddToSlot(msg);
                    }
                    else if (msg.Updatetype == ServerInventoryUpdate.Removal)
                    {
                        Window.RemoveFromSlot(msg);
                    }
                    break;
            }
        }

        /// <summary>
        /// Register a hotkey to open the character menu with
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            OnCharacterMenuKey += OpenMenu;
            IoCManager.Resolve<IEntityManager>().SubscribeEvent<BoundKeyChangedMessage>(OnCharacterMenuKey, this);
        }

        /// <summary>
        /// Hotkey opens the character menu window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OpenMenu(object sender, BoundKeyChangedMessage message)
        {
            if (message.Function == BoundKeyFunctions.OpenCharacterMenu && message.State == BoundKeyState.Down)
            {
                Window.AddToScreen();
                Window.Open();
            }
        }

        public void SendUnequipMessage(Slots slot)
        {
            var unequipmessage = new ClientInventoryMessage(slot, ClientInventoryUpdate.Unequip);
            SendNetworkMessage(unequipmessage);
        }

        public void SendEquipMessage(Slots slot)
        {
            var equipmessage = new ClientInventoryMessage(slot, ClientInventoryUpdate.Equip);
            SendNetworkMessage(equipmessage);
        }

        /// <summary>
        /// Temporary window to hold the basis for inventory hud
        /// </summary>
        private class InventoryWindow : SS14Window
        {
            private int elements_x;

            private GridContainer GridContainer;
            private List<Slots> IndexedSlots;
            private Dictionary<Slots, InventoryButton> InventorySlots = new Dictionary<Slots, InventoryButton>(); //ordered dictionary?
            private ClientInventoryComponent InventoryComponent;

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Inventory/HumanInventory.tscn");

            public InventoryWindow(ClientInventoryComponent inventory)
            {
                InventoryComponent = inventory;

                HideOnClose = true;
            }

            /// <summary>
            /// Creates a grid container filled with slot buttons loaded from an inventory template
            /// </summary>
            /// <param name="TemplateName"></param>
            public void CreateInventory(string TemplateName)
            {
                Type type = AppDomain.CurrentDomain.GetAssemblyByName("Content.Shared").GetType("Content.Shared.GameObjects." + TemplateName);
                Inventory inventory = (Inventory)Activator.CreateInstance(type);

                elements_x = inventory.Columns;

                GridContainer = (GridContainer)Contents.GetChild("PanelContainer").GetChild("CenterContainer").GetChild("GridContainer");
                GridContainer.Columns = elements_x;
                IndexedSlots = new List<Slots>(inventory.SlotMasks);

                foreach (Slots slot in IndexedSlots)
                {
                    InventoryButton newbutton = new InventoryButton(slot);

                    if (slot == Slots.NONE)
                    {
                        //TODO: Re-enable when godot grid container maintains grid with invisible elements
                        //newbutton.Visible = false;
                    }
                    else
                    {
                        //Store slot button and give it the default onpress behavior for empty elements
                        newbutton.GetChild<Button>("Button").OnPressed += AddToInventory;
                        InventorySlots.Add(slot, newbutton);
                    }

                    if (SlotNames.ContainsKey(slot))
                    {
                        newbutton.GetChild<Button>("Button").Text = SlotNames[slot];
                    }

                    GridContainer.AddChild(newbutton);
                }
            }

            /// <summary>
            /// Adds the item we have equipped to the slot texture and prepares the slot button for removal
            /// </summary>
            /// <param name="message"></param>
            public void AddToSlot(ServerInventoryMessage message)
            {
                InventoryButton button = InventorySlots[message.Inventoryslot];
                var entity = IoCManager.Resolve<IEntityManager>().GetEntity(message.EntityUid);

                button.EntityUid = message.EntityUid;
                var container = button.GetChild("CenterContainer");
                button.GetChild<Button>("Button").OnPressed += RemoveFromInventory;
                button.GetChild<Button>("Button").OnPressed -= AddToInventory;

                //Gets entity sprite and assigns it to button texture
                if (entity.TryGetComponent(out IconComponent sprite))
                {
                    var tex = sprite.Icon.Default;

                    var rect = button.GetChild("CenterContainer").GetChild<TextureRect>("TextureRect");

                    if (tex != null)
                    {
                        rect.Texture = tex;
                        rect.Scale = new Vector2(Math.Min(CalculateMinimumSize().X, 32) / tex.Height, Math.Min(CalculateMinimumSize().Y, 32) / tex.Height);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            /// <summary>
            /// Remove element from the UI and update its button to blank texture and prepare for insertion again
            /// </summary>
            /// <param name="message"></param>
            public void RemoveFromSlot(ServerInventoryMessage message)
            {
                InventoryButton button = InventorySlots[message.Inventoryslot];
                button.GetChild("CenterContainer").GetChild<TextureRect>("TextureRect").Texture = null;
                button.EntityUid = EntityUid.Invalid;
                button.GetChild<Button>("Button").OnPressed -= RemoveFromInventory;
                button.GetChild<Button>("Button").OnPressed += AddToInventory;
            }

            private void RemoveFromInventory(BaseButton.ButtonEventArgs args)
            {
                args.Button.Pressed = false;
                var control = (InventoryButton)args.Button.Parent;

                InventoryComponent.SendUnequipMessage(control.Slot);
            }

            private void AddToInventory(BaseButton.ButtonEventArgs args)
            {
                args.Button.Pressed = false;
                var control = (InventoryButton)args.Button.Parent;

                InventoryComponent.SendEquipMessage(control.Slot);
            }
        }

        private class InventoryButton : Control
        {
            public Slots Slot;
            public EntityUid EntityUid;

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Inventory/StorageSlot.tscn");

            public InventoryButton(Slots slot)
            {
                Slot = slot;
            }
        }
    }
}

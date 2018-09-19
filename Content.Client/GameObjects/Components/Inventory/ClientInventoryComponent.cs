using Content.Shared.GameObjects;
using Content.Shared.Input;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Input;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Clothing;
using SS14.Shared.Interfaces.Reflection;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using static Content.Shared.GameObjects.SharedInventoryComponent.ClientInventoryMessage;

namespace Content.Client.GameObjects
{
    public class ClientInventoryComponent : SharedInventoryComponent
    {
        private Dictionary<Slots, IEntity> _slots = new Dictionary<Slots, IEntity>();

        private InventoryWindow _window;
        private string _templateName = "HumanInventory"; //stored for serialization purposes

        private InputCmdHandler _openMenuCmdHandler;
        private Inventory _inventory;

        private ISpriteComponent _sprite;

        public override void OnRemove()
        {
            base.OnRemove();

            _window.Dispose();
        }

        public override void OnAdd()
        {
            base.OnAdd();

            _openMenuCmdHandler = InputCmdHandler.FromDelegate(session => { _window.AddToScreen(); _window.Open(); });
        }

        public override void Initialize()
        {
            base.Initialize();

            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var type = reflectionManager.LooseGetType(_templateName);
            DebugTools.Assert(type != null);
            _inventory = (Inventory)Activator.CreateInstance(type);

            _window = new InventoryWindow(this);
            _window.CreateInventory(_inventory);

            if (Owner.TryGetComponent(out _sprite))
            {
                foreach (var mask in _inventory.SlotMasks.OrderBy(s => _inventory.SlotDrawingOrder(s)))
                {
                    if (mask == Slots.NONE)
                    {
                        continue;
                    }
                    _sprite.LayerMapReserveBlank(mask);
                }
            }

            // Component state already came in but we couldn't set anything visually because, well, we didn't initialize yet.
            foreach (var (slot, entity) in _slots)
            {
                _setSlot(slot, entity);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _templateName, "Template", "HumanInventory");
        }

        public override void HandleComponentState(ComponentState state)
        {
            base.HandleComponentState(state);
            var cast = (InventoryComponentState) state;

            var doneSlots = new HashSet<Slots>();

            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var (slot, entityUid) in cast.Entities)
            {
                if (_slots.ContainsKey(slot))
                {
                    _slots.Remove(slot);
                    _clearSlot(slot);
                }

                var entity = entityManager.GetEntity(entityUid);
                _slots[slot] = entity;
                _setSlot(slot, entity);
                doneSlots.Add(slot);
            }

            foreach (var slot in _slots.Keys.ToList())
            {
                if (!doneSlots.Contains(slot))
                {
                    _clearSlot(slot);
                    _slots.Remove(slot);
                }
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            var inputMgr = IoCManager.Resolve<IInputManager>();
            switch (message)
            {
                case PlayerAttachedMsg _:
                    inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, _openMenuCmdHandler);
                    break;

                case PlayerDetachedMsg _:
                    inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
                    break;
            }
        }

        private void _setSlot(Slots slot, IEntity entity)
        {
            if (_sprite != null && entity.TryGetComponent(out ClothingComponent clothing))
            {
                var flag = SlotMasks[slot];
                var data = clothing.GetEquippedStateInfo(flag);
                if (data == null)
                {
                    _sprite.LayerSetVisible(slot, false);
                }
                else
                {
                    var (rsi, state) = data.Value;
                    _sprite.LayerSetVisible(slot, true);
                    _sprite.LayerSetRSI(slot, rsi);
                    _sprite.LayerSetState(slot, state);
                }
            }

            _window?.AddToSlot(slot, entity);
        }

        private void _clearSlot(Slots slot)
        {
            _window?.RemoveFromSlot(slot);
            _sprite?.LayerSetVisible(slot, false);
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
            ///     Creates a grid container filled with slot buttons loaded from an inventory template
            /// </summary>
            public void CreateInventory(Inventory inventory)
            {
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
            public void AddToSlot(Slots slot, IEntity entity)
            {
                var button = InventorySlots[slot];

                button.EntityUid = entity.Uid;
                button.GetChild<Button>("Button").OnPressed += RemoveFromInventory;
                button.GetChild<Button>("Button").OnPressed -= AddToInventory;

                //Gets entity sprite and assigns it to button texture
                if (entity.TryGetComponent(out ISpriteComponent sprite))
                {
                    //var tex = sprite.Icon.Default;

                    var view = button.GetChild<SpriteView>("SpriteView");
                    view.Sprite = sprite;

                    /*
                    if (tex != null)
                    {
                        rect.Texture = tex;
                        rect.Scale = new Vector2(Math.Min(CalculateMinimumSize().X, 32) / tex.Height, Math.Min(CalculateMinimumSize().Y, 32) / tex.Height);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    */
                }
            }

            /// <summary>
            /// Remove element from the UI and update its button to blank texture and prepare for insertion again
            /// </summary>
            public void RemoveFromSlot(Slots slot)
            {
                var button = InventorySlots[slot];
                button.GetChild<SpriteView>("SpriteView").Sprite = null;
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

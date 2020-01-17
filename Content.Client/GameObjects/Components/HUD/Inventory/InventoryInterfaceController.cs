using System;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects
{
    public abstract class InventoryInterfaceController : IDisposable
    {
#pragma warning disable 649
        [Dependency] protected readonly IGameHud _gameHud;
#pragma warning restore 649

        protected InventoryInterfaceController(ClientInventoryComponent owner)
        {
            Owner = owner;
        }

        public virtual void Initialize()
        {

        }

        public abstract SS14Window Window { get; }
        protected ClientInventoryComponent Owner { get; }

        public virtual void PlayerAttached()
        {
            _gameHud.InventoryButtonVisible = true;
            _gameHud.InventoryButtonToggled = b =>
            {
                if (b)
                {
                    Window.Open();
                }
                else
                {
                    Window.Close();
                }
            };
        }

        public virtual void PlayerDetached()
        {
            _gameHud.InventoryButtonVisible = false;
            Window.Close();
        }

        public virtual void Dispose()
        {
        }

        public virtual void AddToSlot(EquipmentSlotDefines.Slots slot, IEntity entity)
        {
        }

        public virtual void RemoveFromSlot(EquipmentSlotDefines.Slots slot)
        {
        }

        protected virtual void HandleInventoryKeybind(BaseButton.ButtonEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Event.CanFocus)
            {
                UseItemOnInventory(args, slot);
            }
        }

        protected void AddToInventory(BaseButton.ButtonEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (!args.Event.CanFocus)
            {
                return;
            }
            args.Button.Pressed = false;

            Owner.SendEquipMessage(slot);
        }

        protected void UseItemOnInventory(BaseButton.ButtonEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            args.Button.Pressed = false;

            Owner.SendUseMessage(slot);
        }

        protected void OpenStorage(BaseButton.ButtonEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (!args.Event.CanFocus && args.Event.Function != ContentKeyFunctions.ActivateItemInWorld)
            {
                return;
            }
            args.Button.Pressed = false;

            Owner.SendOpenStorageUIMessage(slot);
        }
    }
}

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
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        [field: Dependency] protected IGameHud GameHud { get; }

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
            GameHud.InventoryButtonVisible = true;
            GameHud.InventoryButtonToggled = b =>
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
            GameHud.InventoryButtonVisible = false;
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

        protected void HandleInventoryKeybind(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                OpenStorage(args);
            }
            else if (args.Event.CanFocus)
            {
                UseItemOnInventory(args);
            }
        }

        protected void AddToInventory(BaseButton.ButtonEventArgs args)
        {
            if (!args.Event.CanFocus)
            {
                return;
            }
            args.Button.Pressed = false;
            var control = (InventoryButton) args.Button.Parent;

            Owner.SendEquipMessage(control.Slot);
        }

        protected void UseItemOnInventory(BaseButton.ButtonEventArgs args)
        {
            args.Button.Pressed = false;
            var control = (InventoryButton)args.Button.Parent;

            Owner.SendUseMessage(control.Slot);
        }

        protected void OpenStorage(BaseButton.ButtonEventArgs args)
        {
            if (!args.Event.CanFocus && args.Event.Function != ContentKeyFunctions.ActivateItemInWorld)
            {
                return;
            }
            args.Button.Pressed = false;
            var control = (InventoryButton)args.Button.Parent;

            Owner.SendOpenStorageUIMessage(control.Slot);
        }
    }
}

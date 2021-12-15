using System;
using System.Collections.Generic;
using Content.Client.HUD;
using Content.Client.Items.UI;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;

namespace Content.Client.Inventory
{
    public abstract class InventoryInterfaceController : IDisposable
    {
        [Dependency] protected readonly IGameHud GameHud = default!;

        protected InventoryInterfaceController(ClientInventoryComponent owner)
        {
            Owner = owner;
        }

        public virtual void Initialize()
        {
        }

        public abstract SS14Window? Window { get; }
        protected ClientInventoryComponent Owner { get; }

        public virtual void PlayerAttached()
        {
            GameHud.InventoryButtonVisible = true;
        }

        public virtual void PlayerDetached()
        {
            GameHud.InventoryButtonVisible = false;
        }

        public virtual void Dispose()
        {
        }

        /// <returns>the button controls associated with the
        /// specified slot, if any. Empty if none.</returns>
        public abstract IEnumerable<ItemSlotButton> GetItemSlotButtons(EquipmentSlotDefines.Slots slot);

        public virtual void AddToSlot(EquipmentSlotDefines.Slots slot, EntityUid entity)
        {
        }

        public virtual void HoverInSlot(EquipmentSlotDefines.Slots slot, EntityUid entity, bool fits)
        {
        }

        public virtual void RemoveFromSlot(EquipmentSlotDefines.Slots slot)
        {
        }

        protected virtual void HandleInventoryKeybind(GUIBoundKeyEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                UseItemOnInventory(slot);
            }
        }

        protected void AddToInventory(GUIBoundKeyEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            Owner.SendEquipMessage(slot);
        }

        protected void UseItemOnInventory(EquipmentSlotDefines.Slots slot)
        {
            Owner.SendUseMessage(slot);
        }

        protected void OpenStorage(GUIBoundKeyEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Function != EngineKeyFunctions.UIClick && args.Function != ContentKeyFunctions.ActivateItemInWorld)
            {
                return;
            }

            Owner.SendOpenStorageUIMessage(slot);
        }

        protected void RequestItemHover(EquipmentSlotDefines.Slots slot)
        {
            Owner.SendHoverMessage(slot);
        }
    }
}

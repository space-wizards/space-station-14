#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// Gives components behavior after their entity is put into inventory of a mob that has a SharedActionsComponent,
    /// whether in hand or in an equipment slot (not inside storage containers), regardless of where it came from.
    /// Fires even when moving between slots. Provides some useful fields to reduce boilerplate related to granting item actions.
    ///
    /// As item actions are automatically removed when unequipped, components implementing this do not generally
    /// need to implement logic related to unequipping.
    /// </summary>
    public interface IItemActionsEquipped
    {
        void ItemActionsEquipped(ItemActionsEquippedEventArgs args);
    }

    public class ItemActionsEquippedEventArgs : UserEventArgs
    {
        /// <summary>
        /// Slot equipped to, NONE if equipped to hand
        /// </summary>
        public readonly EquipmentSlotDefines.Slots Slot;
        /// <summary>
        /// Hand equipped to, null if equipped to equip slot.
        /// </summary>
        public readonly SharedHand? Hand;

        /// <summary>
        /// Actions component of the person to whom the item is equipped. Never null.
        /// </summary>
        public readonly SharedActionsComponent UserActionsComponent;

        /// <summary>
        /// True if equipped to hand.
        /// </summary>
        public bool InHand => Hand != null;

        /// <summary>
        /// True if equipped to equip slot
        /// </summary>
        public bool InEquipSlot => !InHand;

        private ItemActionsEquippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot, SharedHand? hand,
            SharedActionsComponent actionsComponent) : base(user)
        {
            Slot = slot;
            Hand = hand;
            UserActionsComponent = actionsComponent;
        }

        public static bool TryCreateFrom(EquippedMessage fromArgs,
            [NotNullWhen(true)] out ItemActionsEquippedEventArgs? outArgs)
        {
            if (!fromArgs.User.TryGetComponent<SharedActionsComponent>(out var actionsComponent))
            {
                outArgs = null;
                return false;
            }

            outArgs = new ItemActionsEquippedEventArgs(fromArgs.User, fromArgs.Slot, null, actionsComponent);
            return true;
        }

        public static bool TryCreateFrom(EquippedHandMessage fromArgs,
            [NotNullWhen(true)] out ItemActionsEquippedEventArgs? outArgs)
        {
            if (!fromArgs.User.TryGetComponent<SharedActionsComponent>(out var actionsComponent))
            {
                outArgs = null;
                return false;
            }

            outArgs = new ItemActionsEquippedEventArgs(fromArgs.User, EquipmentSlotDefines.Slots.NONE, fromArgs.Hand, actionsComponent);
            return true;
        }
    }
}

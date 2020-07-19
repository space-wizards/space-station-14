using System;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when their owner is removed from an inventory slot.
    /// </summary>
    public interface IUnequipped
    {
        void Unequipped(UnequippedEventArgs eventArgs);
    }

    public class UnequippedEventArgs : EventArgs
    {
        public UnequippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Slot = slot;
        }

        public IEntity User { get; }
        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     Raised when removing the entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was equipped.
        /// </summary>
        public IEntity Equipped { get; }

        /// <summary>
        ///     Slot where the item was removed from.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public UnequippedMessage(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Equipped = equipped;
            Slot = slot;
        }
    }


}

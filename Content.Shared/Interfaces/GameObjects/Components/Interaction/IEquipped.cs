using System;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when their owner is put in a non-hand inventory slot,
    ///     regardless of where it came from.
    ///     This DOES NOT fire when putting the owner into a hand slot (<see cref="IHandEquipped"/>), but
    ///     it would fire when moving the owner from a hand slot into a non-hand slot (along with <see cref="IHandUnequipped"/>).
    /// </summary>
    public interface IEquipped
    {
        void Equipped(EquippedEventArgs eventArgs);
    }

    public class EquippedEventArgs : EventArgs
    {
        public EquippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Slot = slot;
        }

        public IEntity User { get; }
        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     Raised when equipping the entity in an inventory slot.
    /// </summary>
    [PublicAPI]
    public class EquippedMessage : EntitySystemMessage
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
        ///     Slot where the item was placed.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public EquippedMessage(IEntity user, IEntity equipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Equipped = equipped;
            Slot = slot;
        }
    }
}

#nullable enable
using System;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when their entity is removed from a non-hand inventory slot,
    ///     regardless of where it's going to. This includes moving the entity from a non-hand slot into a hand slot
    ///     (which would also fire <see cref="IEquippedHand"/>).
    ///
    ///     This DOES NOT fire when removing the entity from a hand slot (<see cref="IUnequippedHand"/>), nor
    ///     does it fire when removing the entity from held/equipped storage.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IUnequipped
    {
        [Obsolete("Use UnequippedMessage instead")]
        void Unequipped(UnequippedEventArgs eventArgs);
    }

    public class UnequippedEventArgs : UserEventArgs
    {
        public UnequippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot) : base(user)
        {
            Slot = slot;
        }

        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     Raised when removing the entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedMessage : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was unequipped.
        /// </summary>
        public IEntity Unequipped { get; }

        /// <summary>
        ///     Slot where the item was removed from.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public UnequippedMessage(IEntity user, IEntity unequipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Unequipped = unequipped;
            Slot = slot;
        }
    }


}

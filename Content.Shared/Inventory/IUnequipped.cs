using System;
using Content.Shared.Hands;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory
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
        public UnequippedEventArgs(EntityUid user, EquipmentSlotDefines.Slots slot) : base(user)
        {
            Slot = slot;
        }

        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     Raised when removing an entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item that was unequipped.
        /// </summary>
        public EntityUid Unequipped { get; }

        /// <summary>
        ///     Slot that the item was removed from.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public UnequippedEvent(EntityUid user, EntityUid unequipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Unequipped = unequipped;
            Slot = slot;
        }
    }


}

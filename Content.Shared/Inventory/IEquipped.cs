using System;
using Content.Shared.Hands;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory
{
    /// <summary>
    ///     This interface gives components behavior when their entity is put in a non-hand inventory slot,
    ///     regardless of where it came from.  This includes moving the entity from a hand slot into a non-hand slot
    ///     (which would also fire <see cref="IUnequippedHand"/>).
    ///
    ///     This DOES NOT fire when putting the entity into a hand slot (<see cref="IEquippedHand"/>), nor
    ///     does it fire when putting the entity into held/equipped storage.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IEquipped
    {
        [Obsolete("Use EquippedMessage instead")]
        void Equipped(EquippedEventArgs eventArgs);
    }

    public abstract class UserEventArgs : EventArgs
    {
        public EntityUid User { get; }

        protected UserEventArgs(EntityUid user)
        {
            User = user;
        }
    }

    public class EquippedEventArgs : UserEventArgs
    {
        public EquippedEventArgs(EntityUid user, EquipmentSlotDefines.Slots slot) : base(user)
        {
            Slot = slot;
        }

        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     Raised when equipping an entity in an inventory slot.
    /// </summary>
    [PublicAPI]
    public class EquippedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item that was equipped.
        /// </summary>
        public EntityUid Equipped { get; }

        /// <summary>
        ///     Slot that the item was placed into.
        /// </summary>
        public EquipmentSlotDefines.Slots Slot { get; }

        public EquippedEvent(EntityUid user, EntityUid equipped, EquipmentSlotDefines.Slots slot)
        {
            User = user;
            Equipped = equipped;
            Slot = slot;
        }
    }
}

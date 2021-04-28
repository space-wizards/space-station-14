#nullable enable
using System;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
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
        public IEntity User { get; }

        protected UserEventArgs(IEntity user)
        {
            User = user;
        }
    }

    public class EquippedEventArgs : UserEventArgs
    {
        public EquippedEventArgs(IEntity user, EquipmentSlotDefines.Slots slot) : base(user)
        {
            Slot = slot;
        }

        public EquipmentSlotDefines.Slots Slot { get; }
    }

    /// <summary>
    ///     Raised when equipping the entity in an inventory slot.
    /// </summary>
    [PublicAPI]
    public class EquippedMessage : HandledEntityEventArgs
    {
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

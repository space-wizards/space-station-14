using System;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when their owner is removed from a hand slot,
    ///     even if it is going into another hand slot (which would also fire <see cref="IEquippedHand"/>).
    ///     This includes moving the owner from a hand slot into a non-hand slot (which would also fire <see cref="IEquipped"/>).
    /// </summary>
    public interface IUnequippedHand
    {
        void UnequippedHand(UnequippedHandEventArgs eventArgs);
    }

    public class UnequippedHandEventArgs : EventArgs
    {
        public UnequippedHandEventArgs(IEntity user, SharedHand hand)
        {
            User = user;
            Hand = hand;
        }

        public IEntity User { get; }
        public SharedHand Hand { get; }
    }

    /// <summary>
    ///     Raised when removing the entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedHandMessage : EntitySystemMessage
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
        ///     Hand the item is removed from.
        /// </summary>
        public SharedHand Hand { get; }

        public UnequippedHandMessage(IEntity user, IEntity equipped, SharedHand hand)
        {
            User = user;
            Equipped = equipped;
            Hand = hand;
        }
    }
}

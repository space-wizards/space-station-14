using System;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Hands
{
    /// <summary>
    ///     This interface gives components behavior when their entity is put in a hand inventory slot,
    ///     even if it came from another hand slot (which would also fire <see cref="IUnequippedHand"/>).
    ///     This includes moving the entity from a non-hand slot into a hand slot
    ///     (which would also fire <see cref="IUnequipped"/>).
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IEquippedHand
    {
        [Obsolete("Use EquippedHandMessage instead")]
        void EquippedHand(EquippedHandEventArgs eventArgs);
    }

    public class EquippedHandEventArgs : UserEventArgs
    {
        public EquippedHandEventArgs(IEntity user, Hand hand) : base(user)
        {
            Hand = hand;
        }

        public Hand Hand { get; }
    }

    /// <summary>
    ///     Raised when putting an entity into a hand slot
    /// </summary>
    [PublicAPI]
    public class EquippedHandEvent : HandledEntityEventArgs
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
        ///     Hand that the item was placed into.
        /// </summary>
        public Hand Hand { get; }

        public EquippedHandEvent(IEntity user, IEntity equipped, Hand hand)
        {
            User = user;
            Equipped = equipped;
            Hand = hand;
        }
    }
}

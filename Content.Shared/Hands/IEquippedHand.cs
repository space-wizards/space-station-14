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
        public EquippedHandEventArgs(EntityUid user, HandState hand) : base(user)
        {
            Hand = hand;
        }

        public HandState Hand { get; }
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
        public EntityUid User { get; }

        /// <summary>
        ///     Item that was equipped.
        /// </summary>
        public EntityUid Equipped { get; }

        /// <summary>
        ///     Hand that the item was placed into.
        /// </summary>
        public HandState Hand { get; }

        public EquippedHandEvent(EntityUid user, EntityUid equipped, HandState hand)
        {
            User = user;
            Equipped = equipped;
            Hand = hand;
        }
    }
}

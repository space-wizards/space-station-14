#nullable enable
using System;
using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
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
        public EquippedHandEventArgs(IEntity user, SharedHand hand) : base(user)
        {
            Hand = hand;
        }

        public SharedHand Hand { get; }
    }

    /// <summary>
    ///     Raised when putting the entity into a hand slot
    /// </summary>
    [PublicAPI]
    public class EquippedHandMessage : HandledEntityEventArgs
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
        ///     Hand the item is going into.
        /// </summary>
        public SharedHand Hand { get; }

        public EquippedHandMessage(IEntity user, IEntity equipped, SharedHand hand)
        {
            User = user;
            Equipped = equipped;
            Hand = hand;
        }
    }
}

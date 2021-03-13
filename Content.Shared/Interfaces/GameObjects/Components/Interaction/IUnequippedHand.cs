#nullable enable
using Content.Shared.GameObjects.Components.Items;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when their entity is removed from a hand slot,
    ///     even if it is going into another hand slot (which would also fire <see cref="IEquippedHand"/>).
    ///     This includes moving the entity from a hand slot into a non-hand slot (which would also fire <see cref="IEquipped"/>).
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IUnequippedHand
    {
        void UnequippedHand(UnequippedHandEventArgs eventArgs);
    }

    public class UnequippedHandEventArgs : UserEventArgs
    {
        public UnequippedHandEventArgs(IEntity user, HandState hand) : base(user)
        {
            Hand = hand;
        }

        public HandState Hand { get; }
    }

    /// <summary>
    ///     Raised when removing the entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedHandMessage : HandledEntityEventArgs
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
        ///     Hand the item is removed from.
        /// </summary>
        public HandState Hand { get; }

        public UnequippedHandMessage(IEntity user, IEntity unequipped, HandState hand)
        {
            User = user;
            Unequipped = unequipped;
            Hand = hand;
        }
    }
}

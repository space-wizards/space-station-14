using SS14.Shared.Interfaces.GameObjects;
using System;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IInteractableComponent : IComponent
    {
        /// <summary>
        ///     Invoked when an entity is clicked with an empty hand.
        /// </summary>
        event EventHandler<AttackHandEventArgs> OnAttackHand;

        /// <summary>
        /// Invoked when an entity is clicked with an item.
        /// </summary>
        event EventHandler<AttackByEventArgs> OnAttackBy;
    }

    public class AttackByEventArgs : EventArgs
    {
        public readonly IEntity Target;
        public readonly IEntity User;
        public readonly IItemComponent Item;
        public readonly string HandIndex;

        public AttackByEventArgs(IEntity target, IEntity user, IItemComponent item, string handIndex)
        {
            Target = target;
            User = user;
            Item = item;
            HandIndex = handIndex;
        }
    }

    public class AttackHandEventArgs : EventArgs
    {
        public readonly IEntity Target;
        public readonly IEntity User;
        public readonly string HandIndex;

        public AttackHandEventArgs(IEntity target, IEntity user, string handIndex)
        {
            Target = target;
            User = user;
            HandIndex = handIndex;
        }
    }
}

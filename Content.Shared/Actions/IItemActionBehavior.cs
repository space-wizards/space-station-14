using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Currently just a marker interface delineating the different possible
    /// types of item action behaviors.
    /// </summary>
    public interface IItemActionBehavior : IExposeData
    {

    }

    /// <summary>
    /// Base class for all item action event args
    /// </summary>
    public abstract class ItemActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;
        /// <summary>
        /// Item being used to perform the action
        /// </summary>
        public readonly IEntity Item;
        /// <summary>
        /// Action being performed
        /// </summary>
        public readonly ItemActionType ActionType;
        /// <summary>
        /// Actions component of the performer.
        /// </summary>
        public readonly SharedActionsComponent PerformerActionsComponent;

        public ItemActionEventArgs(IEntity performer, IEntity item, ItemActionType actionType)
        {
            Performer = performer;
            ActionType = actionType;
            Item = item;
            if (!Performer.TryGetComponent(out PerformerActionsComponent))
            {
                throw new InvalidOperationException($"performer {performer.Name} tried to perform item action {actionType} " +
                                                    $" but the performer had no actions component," +
                                                    " which should never occur");
            }
        }
    }
}

using System;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Actions.Behaviors.Item
{
    /// <summary>
    /// Currently just a marker interface delineating the different possible
    /// types of item action behaviors.
    /// </summary>
    public interface IItemActionBehavior
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
        public readonly EntityUid Performer;
        /// <summary>
        /// Item being used to perform the action
        /// </summary>
        public readonly EntityUid Item;
        /// <summary>
        /// Action being performed
        /// </summary>
        public readonly ItemActionType ActionType;
        /// <summary>
        /// Item actions component of the item.
        /// </summary>
        public readonly ItemActionsComponent? ItemActions;

        public ItemActionEventArgs(EntityUid performer, EntityUid item, ItemActionType actionType)
        {
            Performer = performer;
            ActionType = actionType;
            Item = item;
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Item, out ItemActions))
            {
                throw new InvalidOperationException($"performer {entMan.GetComponent<MetaDataComponent>(performer).EntityName} tried to perform item action {actionType} " +
                                                    $" for item {entMan.GetComponent<MetaDataComponent>(Item).EntityName} but the item had no ItemActionsComponent," +
                                                    " which should never occur");
            }
        }
    }
}

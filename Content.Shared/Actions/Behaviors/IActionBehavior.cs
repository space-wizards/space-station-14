using System;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Actions.Behaviors
{
    /// <summary>
    /// Currently just a marker interface delineating the different possible
    /// types of action behaviors.
    /// </summary>
    public interface IActionBehavior
    {
    }

    /// <summary>
    /// Base class for all action event args
    /// </summary>
    public abstract class ActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly EntityUid Performer;
        /// <summary>
        /// Action being performed
        /// </summary>
        public readonly ActionType ActionType;
        /// <summary>
        /// Actions component of the performer.
        /// </summary>
        public readonly SharedActionsComponent? PerformerActions;

        public ActionEventArgs(EntityUid performer, ActionType actionType)
        {
            Performer = performer;
            ActionType = actionType;
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Performer, out PerformerActions))
            {
                throw new InvalidOperationException($"performer {IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(performer).EntityName} tried to perform action {actionType} " +
                                                    $" but the performer had no actions component," +
                                                    " which should never occur");
            }
        }
    }
}

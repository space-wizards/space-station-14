#nullable enable
using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Shared.Actions
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
        public readonly IEntity Performer;
        /// <summary>
        /// Action being performed
        /// </summary>
        public readonly ActionType ActionType;
        /// <summary>
        /// Actions component of the performer.
        /// </summary>
        public readonly SharedActionsComponent? PerformerActions;

        public ActionEventArgs(IEntity performer, ActionType actionType)
        {
            Performer = performer;
            ActionType = actionType;
            if (!Performer.TryGetComponent(out PerformerActions))
            {
                throw new InvalidOperationException($"performer {performer.Name} tried to perform action {actionType} " +
                                                    $" but the performer had no actions component," +
                                                    " which should never occur");
            }
        }
    }
}

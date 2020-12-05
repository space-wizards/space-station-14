using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Currently just a marker interface delineating the different possible
    /// types of action behaviors.
    /// </summary>
    public interface IActionBehavior : IExposeData
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

        public ActionEventArgs(IEntity performer, ActionType actionType)
        {
            Performer = performer;
            ActionType = actionType;
        }
    }
}

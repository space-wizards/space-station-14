using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Item action which does something immediately when used and has
    /// no target.
    /// </summary>
    public interface IInstantItemAction : IActionBehavior
    {

        /// <summary>
        /// Invoked when the instant action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoInstantAction(InstantItemActionEventArgs args);
    }

    public class InstantItemActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;

        /// <summary>
        /// Item being used to perform the action.
        /// </summary>
        public readonly IEntity Item;

        public InstantItemActionEventArgs(IEntity performer, IEntity item)
        {
            Performer = performer;
            Item = item;
        }
    }
}

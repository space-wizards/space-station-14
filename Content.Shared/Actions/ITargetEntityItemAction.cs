using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Item action which is used on a targeted entity.
    /// </summary>
    public interface ITargetEntityItemAction : IActionBehavior
    {
        /// <summary>
        /// Invoked when the target entity action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoTargetEntityAction(TargetEntityItemActionEventArgs args);
    }

    public class TargetEntityItemActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;

        /// <summary>
        /// Entity being targeted
        /// </summary>
        public readonly IEntity Target;

        /// <summary>
        /// Item being used to perform the action.
        /// </summary>
        public readonly IEntity Item;

        public TargetEntityItemActionEventArgs(IEntity performer, IEntity target, IEntity item)
        {
            Performer = performer;
            Target = target;
            Item = item;
        }
    }
}

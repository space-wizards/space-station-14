using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Action which is used on a targeted entity.
    /// </summary>
    public interface ITargetEntityAction : IActionBehavior
    {
        /// <summary>
        /// Invoked when the target entity action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoTargetEntityAction(TargetEntitytActionEventArgs args);
    }

    public class TargetEntitytActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;

        /// <summary>
        /// Entity being targeted
        /// </summary>
        public readonly IEntity Target;

        public TargetEntitytActionEventArgs(IEntity performer, IEntity target)
        {
            Performer = performer;
            Target = target;
        }
    }
}

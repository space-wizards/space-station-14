using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Action which does something immediately when used and has
    /// no target.
    /// </summary>
    public interface IInstantAction : IActionBehavior
    {

        void DoInstantAction(InstantActionEventArgs args);
    }

    public class InstantActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;

        public InstantActionEventArgs(IEntity performer)
        {
            Performer = performer;
        }
    }
}

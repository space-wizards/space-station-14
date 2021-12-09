using Robust.Shared.GameObjects;

namespace Content.Shared.Actions.Behaviors
{
    /// <summary>
    /// Action which does something immediately when used and has
    /// no target.
    /// </summary>
    public interface IInstantAction : IActionBehavior
    {

        /// <summary>
        /// Invoked when the instant action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoInstantAction(InstantActionEventArgs args);
    }

    public class InstantActionEventArgs : ActionEventArgs
    {
        public InstantActionEventArgs(EntityUid performer, ActionType actionType) : base(performer, actionType)
        {
        }
    }
}

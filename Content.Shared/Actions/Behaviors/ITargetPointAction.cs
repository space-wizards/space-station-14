using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Actions.Behaviors
{
    /// <summary>
    /// Action which requires the user to select a target point, which
    /// does not necessarily have an entity on it.
    /// </summary>
    public interface ITargetPointAction : IActionBehavior
    {
        /// <summary>
        /// Invoked when the target point action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoTargetPointAction(TargetPointActionEventArgs args);
    }

    public class TargetPointActionEventArgs : ActionEventArgs
    {
        /// <summary>
        /// Local coordinates of the targeted position.
        /// </summary>
        public readonly EntityCoordinates Target;

        public TargetPointActionEventArgs(EntityUid performer, EntityCoordinates target, ActionType actionType)
            : base(performer, actionType)
        {
            Target = target;
        }
    }
}

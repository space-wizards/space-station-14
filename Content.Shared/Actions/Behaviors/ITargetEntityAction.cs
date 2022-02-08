using Robust.Shared.GameObjects;

namespace Content.Shared.Actions.Behaviors
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
        void DoTargetEntityAction(TargetEntityActionEventArgs args);
    }

    public class TargetEntityActionEventArgs : ActionEventArgs
    {
        /// <summary>
        /// Entity being targeted
        /// </summary>
        public readonly EntityUid Target;

        public TargetEntityActionEventArgs(EntityUid performer, ActionType actionType, EntityUid target) :
            base(performer, actionType)
        {
            Target = target;
        }
    }
}

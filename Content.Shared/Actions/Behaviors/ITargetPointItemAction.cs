using Content.Shared.Actions.Behaviors.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Actions.Behaviors
{
    /// <summary>
    /// Item action which requires the user to select a target point, which
    /// does not necessarily have an entity on it.
    /// </summary>
    public interface ITargetPointItemAction : IItemActionBehavior
    {
        /// <summary>
        /// Invoked when the target point action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoTargetPointAction(TargetPointItemActionEventArgs args);
    }

    public sealed class TargetPointItemActionEventArgs : ItemActionEventArgs
    {
        /// <summary>
        /// Local coordinates of the targeted position.
        /// </summary>
        public readonly EntityCoordinates Target;

        public TargetPointItemActionEventArgs(EntityUid performer, EntityCoordinates target, EntityUid item,
            ItemActionType actionType) : base(performer, item, actionType)
        {
            Target = target;
        }
    }
}

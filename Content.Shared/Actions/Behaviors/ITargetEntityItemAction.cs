using Content.Shared.Actions.Behaviors.Item;
using Robust.Shared.GameObjects;

namespace Content.Shared.Actions.Behaviors
{
    /// <summary>
    /// Item action which is used on a targeted entity.
    /// </summary>
    public interface ITargetEntityItemAction : IItemActionBehavior
    {
        /// <summary>
        /// Invoked when the target entity action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoTargetEntityAction(TargetEntityItemActionEventArgs args);
    }

    public sealed class TargetEntityItemActionEventArgs : ItemActionEventArgs
    {
        /// <summary>
        /// Entity being targeted
        /// </summary>
        public readonly EntityUid Target;

        public TargetEntityItemActionEventArgs(EntityUid performer, EntityUid target, EntityUid item,
            ItemActionType actionType) : base(performer, item, actionType)
        {
            Target = target;

        }
    }
}

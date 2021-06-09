#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Actions.Behaviors.Item
{
    /// <summary>
    /// Item action which does something immediately when used and has
    /// no target.
    /// </summary>
    public interface IInstantItemAction : IItemActionBehavior
    {

        /// <summary>
        /// Invoked when the instant action should be performed.
        /// Implementation should perform the server side logic of the action.
        /// </summary>
        void DoInstantAction(InstantItemActionEventArgs args);
    }

    public class InstantItemActionEventArgs : ItemActionEventArgs
    {
        public InstantItemActionEventArgs(IEntity performer, IEntity item, ItemActionType actionType) :
            base(performer, item, actionType)
        {
        }
    }
}

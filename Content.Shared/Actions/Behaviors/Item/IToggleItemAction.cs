using Robust.Shared.GameObjects;

namespace Content.Shared.Actions.Behaviors.Item
{
    /// <summary>
    /// Item action which can be toggled on and off
    /// </summary>
    public interface IToggleItemAction : IItemActionBehavior
    {
        /// <summary>
        /// Invoked when the action will be toggled on/off.
        /// Implementation should perform the server side logic of whatever
        /// happens when it is toggled on / off.
        /// </summary>
        /// <returns>true if the attempt to toggle was successful, meaning the state should be toggled to the desired value.
        /// False to leave toggle status unchanged. This is NOT returning the new toggle status, it is only returning
        /// whether the attempt to toggle to the indicated status was successful.
        ///
        /// Note that it's still okay if the implementation directly modifies toggle status via ItemActionsComponent,
        /// this is just an additional level of safety to ensure implementations will always
        /// explicitly indicate if the toggle status should be changed.</returns>
        bool DoToggleAction(ToggleItemActionEventArgs args);
    }

    public sealed class ToggleItemActionEventArgs : ItemActionEventArgs
    {
        /// <summary>
        /// True if the toggle was toggled on, false if it was toggled off
        /// </summary>
        public readonly bool ToggledOn;
        /// <summary>
        /// Opposite of ToggledOn
        /// </summary>
        public bool ToggledOff => !ToggledOn;

        public ToggleItemActionEventArgs(EntityUid performer, bool toggledOn, EntityUid item,
            ItemActionType actionType) : base(performer, item, actionType)
        {
            ToggledOn = toggledOn;
        }
    }
}

using Robust.Shared.GameObjects;

namespace Content.Shared.Actions.Behaviors
{
    /// <summary>
    /// Action which can be toggled on and off
    /// </summary>
    public interface IToggleAction : IActionBehavior
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
        /// Note that it's still okay if the implementation directly modifies toggle status via SharedActionsComponent,
        /// this is just an additional level of safety to ensure implementations will always
        /// explicitly indicate if the toggle status should be changed.</returns>
        bool DoToggleAction(ToggleActionEventArgs args);
    }

    public class ToggleActionEventArgs : ActionEventArgs
    {
        /// <summary>
        /// True if the toggle is attempting to be toggled on, false if attempting to toggle off
        /// </summary>
        public readonly bool ToggledOn;
        /// <summary>
        /// Opposite of ToggledOn
        /// </summary>
        public bool ToggledOff => !ToggledOn;

        public ToggleActionEventArgs(EntityUid performer, ActionType actionType, bool toggledOn) : base(performer, actionType)
        {
            ToggledOn = toggledOn;
        }
    }
}

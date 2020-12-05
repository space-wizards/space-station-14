using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Action which can be toggled on and off
    /// </summary>
    public interface IToggleAction : IActionBehavior
    {
        /// <summary>
        /// Invoked after the action is toggled on/off.
        /// Implementation should perform the server side logic of whatever
        /// happens when it is toggled on / off.
        /// </summary>
        void DoToggleAction(ToggleActionEventArgs args);
    }

    public class ToggleActionEventArgs : ActionEventArgs
    {
        /// <summary>
        /// True if the toggle was toggled on, false if it was toggled off
        /// </summary>
        public readonly bool ToggledOn;
        /// <summary>
        /// Opposite of ToggledOn
        /// </summary>
        public bool ToggledOff => !ToggledOn;

        public ToggleActionEventArgs(IEntity performer, ActionType actionType, bool toggledOn) : base(performer, actionType)
        {
            ToggledOn = toggledOn;
        }
    }
}

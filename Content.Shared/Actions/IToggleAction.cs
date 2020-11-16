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
        /// Invoked when the action is being toggled on/off.
        /// Implementation should perform the server side logic of whatever
        /// happens when it is toggled on / off.
        /// </summary>
        void DoToggleAction(ToggleActionEventArgs args);
    }

    public class ToggleActionEventArgs : EventArgs
    {
        /// <summary>
        /// Entity performing the action.
        /// </summary>
        public readonly IEntity Performer;
        /// <summary>
        /// True if the toggle is being toggled on, false if being toggled off
        /// </summary>
        public readonly bool ToggledOn;
        /// <summary>
        /// Opposite of ToggledOn
        /// </summary>
        public bool ToggledOff => !ToggledOn;

        public ToggleActionEventArgs(IEntity performer, bool toggledOn)
        {
            Performer = performer;
            ToggledOn = toggledOn;
        }
    }
}

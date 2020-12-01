using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Item action which can be toggled on and off
    /// </summary>
    public interface IToggleItemAction : IActionBehavior
    {
        /// <summary>
        /// Invoked when the action is being toggled on/off.
        /// Implementation should perform the server side logic of whatever
        /// happens when it is toggled on / off.
        /// </summary>
        void DoToggleAction(ToggleItemActionEventArgs args);
    }

    public class ToggleItemActionEventArgs : EventArgs
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
        /// <summary>
        /// Item being used to perform the action.
        /// </summary>
        public readonly IEntity Item;

        public ToggleItemActionEventArgs(IEntity performer, bool toggledOn, IEntity item)
        {
            Performer = performer;
            ToggledOn = toggledOn;
            Item = item;
        }
    }
}

using Robust.Client.UserInterface;

namespace Content.Client.Items.Components
{
    /// <summary>
    ///     Allows a component to provide status tooltips next to the hands interface.
    /// </summary>
    public interface IItemStatus
    {
        /// <summary>
        ///     Called to get a control that represents the status for this component.
        /// </summary>
        /// <returns>
        ///     The control to render as status.
        /// </returns>
        public Control MakeControl();

        /// <summary>
        ///     Called when the item no longer needs this status (say, dropped from hand)
        /// </summary>
        /// <remarks>
        /// <para>
        ///     Useful to allow you to drop the control for the GC, if you need to.
        /// </para>
        /// <para>
        ///     Note that this may be called after a second invocation of <see cref="MakeControl"/> (for example if the user switches the item between two hands).
        /// </para>
        /// </remarks>
        public void DestroyControl(Control control)
        {
        }
    }
}

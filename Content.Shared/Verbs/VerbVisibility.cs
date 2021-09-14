
namespace Content.Shared.Verbs
{
    /// <summary>
    /// Possible states of visibility for the verb in the right click menu.
    /// </summary>
    public enum VerbVisibility
    {
        /// <summary>
        /// The verb will be listed in the right click menu.
        /// </summary>
        Visible,

        /// <summary>
        /// The verb will be listed, but it will be grayed out and unable to be clicked on.
        /// </summary>
        Disabled,

        /// <summary>
        /// The verb will not be listed in the right click menu.
        /// </summary>
        Invisible
    }
}

using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.Popups
{
    public static class SharedPopupExtensions
    {
        /// <summary>
        ///     Pops up a message at the location of <see cref="source"/> for
        ///     <see cref="viewer"/> alone to see.
        /// </summary>
        /// <param name="source">The entity above which the message will appear.</param>
        /// <param name="viewer">The entity that will see the message.</param>
        /// <param name="message">The message to show.</param>
        [Obsolete("Use PopupSystem.PopupEntity instead.")]
        public static void PopupMessage(this EntityUid source, EntityUid viewer, string message)
        {
            var popupSystem = EntitySystem.Get<SharedPopupSystem>();

            popupSystem.PopupEntity(message, source, viewer);
        }

        /// <summary>
        ///     Pops up a message at the given entity's location for it alone to see.
        /// </summary>
        /// <param name="viewer">The entity that will see the message.</param>
        /// <param name="message">The message to be seen.</param>
        [Obsolete("Use PopupSystem.PopupEntity instead.")]
        public static void PopupMessage(this EntityUid viewer, string message)
        {
            viewer.PopupMessage(viewer, message);
        }

        /// <summary>
        ///     Makes a string of text float up from a client's cursor.
        /// </summary>
        /// <param name="viewer">
        ///     The client attached entity that the message is being sent to.
        /// </param>
        /// <param name="message">Text contents of the message.</param>
        [Obsolete("Use PopupSystem.PopupCursor instead.")]
        public static void PopupMessageCursor(this EntityUid viewer, string message)
        {
            var popupSystem = EntitySystem.Get<SharedPopupSystem>();
            popupSystem.PopupCursor(message, viewer);
        }
    }
}

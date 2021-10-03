using Robust.Shared.GameObjects;
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
        public static void PopupMessage(this IEntity source, IEntity viewer, string message)
        {
            var popupSystem = EntitySystem.Get<SharedPopupSystem>();

            popupSystem.PopupEntity(message, source.Uid, Filter.Entities(viewer.Uid));
        }

        /// <summary>
        ///     Pops up a message at the given entity's location for it alone to see.
        /// </summary>
        /// <param name="viewer">The entity that will see the message.</param>
        /// <param name="message">The message to be seen.</param>
        public static void PopupMessage(this IEntity viewer, string message)
        {
            viewer.PopupMessage(viewer, message);
        }

        /// <summary>
        /// Makes a string of text float up from a location on a grid.
        /// </summary>
        /// <param name="coordinates">Location on a grid that the message floats up from.</param>
        /// <param name="viewer">The client attached entity that the message is being sent to.</param>
        /// <param name="message">Text contents of the message.</param>
        public static void PopupMessage(this EntityCoordinates coordinates, IEntity viewer, string message)
        {
            var popupSystem = EntitySystem.Get<SharedPopupSystem>();
            popupSystem.PopupCoordinates(message, coordinates, Filter.Entities(viewer.Uid));
        }

        /// <summary>
        ///     Makes a string of text float up from a client's cursor.
        /// </summary>
        /// <param name="viewer">
        ///     The client attached entity that the message is being sent to.
        /// </param>
        /// <param name="message">Text contents of the message.</param>
        public static void PopupMessageCursor(this IEntity viewer, string message)
        {
            var popupSystem = EntitySystem.Get<SharedPopupSystem>();
            popupSystem.PopupCursor(message, Filter.Entities(viewer.Uid));
        }
    }
}

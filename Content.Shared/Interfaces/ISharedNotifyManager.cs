using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces
{
    /// <summary>
    /// Allows the ability to create floating text messages at locations in the world.
    /// </summary>
    public interface ISharedNotifyManager
    {
        /// <summary>
        /// Makes a string of text float up from an entity.
        /// </summary>
        /// <param name="source">The entity that the message is floating up from.</param>
        /// <param name="viewer">The client attached entity that the message is being sent to.</param>
        /// <param name="message">Text contents of the message.</param>
        void PopupMessage(IEntity source, IEntity viewer, string message);

        /// <summary>
        /// Makes a string of text float up from a location on a grid.
        /// </summary>
        /// <param name="coordinates">Location on a grid that the message floats up from.</param>
        /// <param name="viewer">The client attached entity that the message is being sent to.</param>
        /// <param name="message">Text contents of the message.</param>
        void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message);

        /// <summary>
        /// Makes a string of text float up from a client's cursor.
        /// </summary>
        /// <param name="viewer">The client attached entity that the message is being sent to.</param>
        /// <param name="message">Text contents of the message.</param>
        void PopupMessageCursor(IEntity viewer, string message);
    }

    public static class SharedNotifyExtensions
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
            var notifyManager = IoCManager.Resolve<ISharedNotifyManager>();
            notifyManager.PopupMessage(source, viewer, message);
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
        public static void PopupMessage(this GridCoordinates coordinates, IEntity viewer, string message)
        {
            var notifyManager = IoCManager.Resolve<ISharedNotifyManager>();
            notifyManager.PopupMessage(coordinates, viewer, message);
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
            var notifyManager = IoCManager.Resolve<ISharedNotifyManager>();
            notifyManager.PopupMessageCursor(viewer, message);
        }
    }
}

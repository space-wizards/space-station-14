using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Popups
{
    /// <summary>
    ///     System for displaying small text popups on users' screens.
    /// </summary>
    public abstract class SharedPopupSystem : EntitySystem
    {
        /// <summary>
        ///     Shows a popup on the users' cursors.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="filter">Filter for the players that will see the popup.</param>
        public abstract void PopupCursor(string message, Filter filter);

        /// <summary>
        ///     Shows a popup at a world location.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="coordinates">The coordinates where to display the message.</param>
        /// <param name="filter">Filter for the players that will see the popup.</param>
        public abstract void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter);

        /// <summary>
        ///     Shows a popup above an entity.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="uid">The UID of the entity.</param>
        /// <param name="filter">Filter for the players that will see the popup.</param>
        public abstract void PopupEntity(string message, EntityUid uid, Filter filter);
    }

    /// <summary>
    ///     Common base for all popup network events.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class PopupEvent : EntityEventArgs
    {
        public string Message { get; }

        protected PopupEvent(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    ///     Network event for displaying a popup on the user's cursor.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupCursorEvent : PopupEvent
    {
        public PopupCursorEvent(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Network event for displaying a popup at a world location.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupCoordinatesEvent : PopupEvent
    {
        public EntityCoordinates Coordinates { get; }

        public PopupCoordinatesEvent(string message, EntityCoordinates coordinates) : base(message)
        {
            Coordinates = coordinates;
        }
    }

    /// <summary>
    ///     Network event for displaying a popup above an entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupEntityEvent : PopupEvent
    {
        public EntityUid Uid { get; }

        public PopupEntityEvent(string message, EntityUid uid) : base(message)
        {
            Uid = uid;
        }
    }
}

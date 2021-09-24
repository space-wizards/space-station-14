using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Popups
{
    public abstract class SharedPopupSystem : EntitySystem
    {
        public abstract void PopupCursor(string message, Filter filter);
        public abstract void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter);
        public abstract void PopupEntity(string message, EntityUid uid, Filter filter);

        public abstract Filter GetFilterFromEntity(IEntity entity);
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
    public class PopupCursorEvent : PopupEvent
    {
        public PopupCursorEvent(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Network event for displaying a popup on a world position.
    /// </summary>
    [Serializable, NetSerializable]
    public class PopupCoordinatesEvent : PopupEvent
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
    public class PopupEntityEvent : PopupEvent
    {
        public EntityUid Uid { get; }

        public PopupEntityEvent(string message, EntityUid uid) : base(message)
        {
            Uid = uid;
        }
    }
}

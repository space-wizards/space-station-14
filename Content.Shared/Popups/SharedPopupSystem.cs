using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Popups
{
    /// <summary>
    ///     System for displaying small text popups on users' screens.
    /// </summary>
    public abstract class SharedPopupSystem : EntitySystem
    {
        /// <summary>
        ///     Shows a popup at the local users' cursor. Does nothing on the server.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCursor(string message, PopupType type = PopupType.Small);

        /// <summary>
        ///     Shows a popup at a users' cursor.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="recipient">Client that will see this popup.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCursor(string message, ICommonSession recipient, PopupType type = PopupType.Small);

        /// <summary>
        ///     Shows a popup at a users' cursor.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="recipient">Client that will see this popup.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCursor(string message, EntityUid recipient, PopupType type = PopupType.Small);

        /// <summary>
        ///     Shows a popup at a world location to every entity in PVS range.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="coordinates">The coordinates where to display the message.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupCoordinates(string message, EntityCoordinates coordinates, PopupType type = PopupType.Small);

        /// <summary>
        ///     Filtered variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/>, which should only be used
        ///     if the filtering has to be more specific than simply PVS range based.
        /// </summary>
        /// <param name="filter">Filter for the players that will see the popup.</param>
        /// <param name="recordReplay">If true, this pop-up will be considered as a globally visible pop-up that gets shown during replays.</param>
        public abstract void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter, bool recordReplay, PopupType type = PopupType.Small);

        /// <summary>
        ///     Variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/> that sends a pop-up to the player attached to some entity.
        /// </summary>
        public abstract void PopupCoordinates(string message, EntityCoordinates coordinates, EntityUid recipient, PopupType type = PopupType.Small);

        /// <summary>
        ///     Variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/> that sends a pop-up to a specific player.
        /// </summary>
        public abstract void PopupCoordinates(string message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small);

        /// <summary>
        ///     Shows a popup above an entity for every player in pvs range.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="uid">The UID of the entity.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public abstract void PopupEntity(string message, EntityUid uid, PopupType type=PopupType.Small);

        /// <summary>
        ///     Variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/> that shoes the popup only to some specific client.
        /// </summary>
        public abstract void PopupEntity(string message, EntityUid uid, EntityUid recipient, PopupType type = PopupType.Small);

        /// <summary>
        ///     Variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/> that shoes the popup only to some specific client.
        /// </summary>
        public abstract void PopupEntity(string message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small);

        /// <summary>
        ///     Filtered variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/>, which should only be used
        ///     if the filtering has to be more specific than simply PVS range based.
        /// </summary>
        public abstract void PopupEntity(string message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small);
    }

    /// <summary>
    ///     Common base for all popup network events.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class PopupEvent : EntityEventArgs
    {
        public string Message { get; }

        public PopupType Type { get; }

        protected PopupEvent(string message, PopupType type)
        {
            Message = message;
            Type = type;
        }
    }

    /// <summary>
    ///     Network event for displaying a popup on the user's cursor.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PopupCursorEvent : PopupEvent
    {
        public PopupCursorEvent(string message, PopupType type) : base(message, type)
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

        public PopupCoordinatesEvent(string message, PopupType type, EntityCoordinates coordinates) : base(message, type)
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

        public PopupEntityEvent(string message, PopupType type, EntityUid uid) : base(message, type)
        {
            Uid = uid;
        }
    }

    /// <summary>
    ///     Used to determine how a popup should appear visually to the client. Caution variants simply have a red color.
    /// </summary>
    /// <remarks>
    ///     Actions which can fail or succeed should use a smaller popup for failure and a larger popup for success.
    ///     Actions which have different popups for the user vs. others should use a larger popup for the user and a smaller popup for others.
    ///     Actions which result in harm or are otherwise dangerous should always show as the caution variant.
    /// </remarks>
    [Serializable, NetSerializable]
    public enum PopupType : byte
    {
        /// <summary>
        ///     Small popups are the default, and denote actions that may be spammable or are otherwise unimportant.
        /// </summary>
        Small,
        SmallCaution,
        /// <summary>
        ///     Medium popups should be used for actions which are not spammable but may not be particularly important.
        /// </summary>
        Medium,
        MediumCaution,
        /// <summary>
        ///     Large popups should be used for actions which may be important or very important to one or more users,
        ///     but is not life-threatening.
        /// </summary>
        Large,
        LargeCaution
    }
}

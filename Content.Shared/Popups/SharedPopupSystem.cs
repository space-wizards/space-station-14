using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Popups;

/// <summary>
/// System for displaying small text popups on users' screens.
/// </summary>
public abstract partial class SharedPopupSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    /// <summary>
    /// Shows a popup at a user's cursor.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="recipient">The entity whose attached player will see the popup.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small);

    /// <summary>
    /// Shows a popup at a user's cursor.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="recipient">The player session that will see the popup.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small);

    /// <summary>
    /// Shows a popup at some users' cursors.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="filter">Filter for the clients that will see this popup.</param>
    /// <param name="recordReplay">If true, this pop-up will be considered as a globally visible pop-up that gets shown during replays.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupCursor(string? message, Filter filter, bool recordReplay, PopupType type = PopupType.Small);

    /// <summary>
    /// Shows a popup at a world location to every entity in PVS range.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="coordinates">The coordinates where to display the message.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    /// <param name="predictionKey">Additional key used to uniquely identify this popup event for prediction purposes.</param>
    /// <remarks>
    /// In case your popup is predicted and may show up multiple times at different locations in a single tick, for each popup you should use a different prediction key,
    /// which server and client have to agree on. Otherwise, the client may ignore some of the popups.
    /// This is needed because the coordinates themselves cannot be part of the PredictionInstance, since they slightly differ between server and client due to
    /// floating point precision issues and predictive movement, so two popups at different locations with the same message in the same tick would be considered identical.
    /// The most common use case for this is when you delete an entity and spawn a popup at its location. For this you can use the NetEntity ID of the deleted entity as the prediction key.
    /// </remarks>
    public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small, int predictionKey = 0);

    /// <summary>
    /// Variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/> that sends a popup to the player attached to some entity.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="coordinates">The coordinates where to display the message.</param>
    /// <param name="recipient">The entity whose attached player will see the popup.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    /// <param name="predictionKey">Additional key used to uniquely identify this popup event for prediction purposes.</param>
    public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small, int predictionKey = 0);

    /// <summary>
    /// Variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/> that sends a popup to a specific player.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="coordinates">The coordinates where to display the message.</param>
    /// <param name="recipient">The player session that will see the popup.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    /// <param name="predictionKey">Additional key used to uniquely identify this popup event for prediction purposes.</param>
    public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small, int predictionKey = 0);

    /// <summary>
    /// Filtered variant of <see cref="PopupCoordinates(string, EntityCoordinates, PopupType)"/>, which should only be used
    /// if the filtering has to be more specific than simply PVS range based.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="coordinates">The coordinates where to display the message.</param>
    /// <param name="filter">Filter for the clients that will see this popup.</param>
    /// <param name="recordReplay">If true, this pop-up will be considered as a globally visible pop-up that gets shown during replays.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    /// <param name="predictionKey">Additional key used to uniquely identify this popup event for prediction purposes.</param>
    public abstract void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool recordReplay, PopupType type = PopupType.Small, int predictionKey = 0);

    /// <summary>
    /// Shows a popup above an entity for every player in PVS range.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="uid">The entity above which to display the popup.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small);

    /// <summary>
    /// Variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/> that shows the popup only to some specific client.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="uid">The entity above which to display the popup.</param>
    /// <param name="recipient">The entity whose attached player will see the popup.</param
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small);

    /// <summary>
    /// Variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/> that shows the popup only to some specific client.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="uid">The entity above which to display the popup.</param>
    /// <param name="recipient">The player session that will see the popup.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small);

    /// <summary>
    /// Filtered variant of <see cref="PopupEntity(string, EntityUid, PopupType)"/>, which should only be used
    /// if the filtering has to be more specific than simply PVS range based.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="uid">The entity above which to display the popup.</param>
    /// <param name="filter">Filter for the clients that will see this popup.</param>
    /// <param name="recordReplay">If true, this pop-up will be considered as a globally visible pop-up that gets shown during replays.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param>
    public abstract void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small);

    /// <summary>
    /// Variant of <see cref="PopupEntity(string?, EntityUid, PopupType)"/> that displays <paramref name="recipientMessage"/>
    /// to the recipient and <paramref name="othersMessage"/> to everyone else in PVS range.
    /// </summary>
    /// <param name="recipientMessage">The message to display to the recipient.</param>
    /// <param name="othersMessage">The message to display to everyone else in PVS range.</param>
    /// <param name="uid">The entity above which to display the popup.</param>
    /// <param name="recipient">The entity whose attached player will see the recipient message.</param>
    /// <param name="type">Used to customize how this popup should appear visually.</param
    public void PopupEntity(string? recipientMessage,
        string? othersMessage,
        EntityUid uid,
        EntityUid? recipient,
        PopupType type = PopupType.Small)
    {
        if (recipient.HasValue)
        {
            PopupEntity(othersMessage, uid, Filter.PvsExcept(recipient.Value), true, type);
            PopupEntity(recipientMessage, uid, recipient.Value, type);
        }
        else
        {
            PopupEntity(othersMessage, uid, type);
        }
    }

    [Obsolete("Popups are automatically predicted now, just call PopupCursor and the client will handle prediction.")]
    public void PopupPredictedCursor(string? message, EntityUid recipient, PopupType type = PopupType.Small)
    {
        PopupCursor(message, recipient, type);
    }

    [Obsolete("Popups are automatically predicted now, just call PopupCursor and the client will handle prediction.")]
    public void PopupPredictedCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
    {
        PopupCursor(message, recipient, type);
    }

    [Obsolete("Popups are automatically predicted now, just call PopupCoordinates and the client will handle prediction.")]
    public void PopupPredictedCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        PopupCoordinates(message, coordinates, type); // The recipent was only used for prediction reasons, not as a filter, so we ignore it here.
    }

    [Obsolete("Popups are automatically predicted now, just call PopupEntity and the client will handle prediction.")]
    public void PopupClient(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        if (recipient == null)
            return;

        PopupEntity(message, recipient.Value, recipient, type); // Only show the popup to the recipient, since this was the original behavior.
    }

    [Obsolete("Popups are automatically predicted now, just call PopupEntity and the client will handle prediction.")]
    public void PopupClient(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        PopupEntity(message, uid, recipient, type); // Only show the popup to the recipient, since this was the original behavior.
    }

    [Obsolete("Popups are automatically predicted now, just call PopupCoordinates and the client will handle prediction.")]
    public void PopupClient(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        PopupCoordinates(message, coordinates, recipient, type); // Only show the popup to the recipient, since this was the original behavior.
    }

    [Obsolete("Popups are automatically predicted now, just call PopupEntity and the client will handle prediction.")]
    public void PopupPredicted(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        PopupEntity(message, uid, type); // The recipent was only used for prediction reasons, not as a filter, so we ignore it here.
    }

    [Obsolete("Popups are automatically predicted now, just call PopupEntity and the client will handle prediction.")]
    public void PopupPredicted(string? message, EntityUid uid, EntityUid? recipient, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
    {
        PopupEntity(message, uid, filter, recordReplay, type); // The recipent was only used for prediction reasons, not as a filter, so we ignore it here.
    }

    [Obsolete("Popups are automatically predicted now, just call PopupEntity and the client will handle prediction.")]
    public void PopupPredicted(string? recipientMessage, string? othersMessage, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        PopupEntity(recipientMessage, othersMessage, uid, recipient, type);
    }
}

/// <summary>
/// Common base for all popup network events.
/// </summary>
[Serializable, NetSerializable]
public abstract class PopupEvent(string message, PopupType type, GameTick tick) : EntityEventArgs
{
    /// <summary>
    /// The message to display.
    /// </summary>
    public string Message = message;

    /// <summary>
    /// The type of the popup.
    /// </summary>
    public PopupType Type = type;

    /// <summary>
    /// The game tick at which the popup was created.
    /// </summary>
    public GameTick Tick = tick;
}

/// <summary>
/// Interface for a prediction instance of a popup event.
/// Used to keep track if a popup has already been predicted and displayed on the client side, to avoid duplicate popups.
/// </summary>
public interface IPopupPredictionInstance
{
    /// <summary>
    /// The game tick at which the popup was created.
    /// </summary>
    GameTick Tick { get; }
}

/// <summary>
/// Network event for displaying a popup on the user's cursor.
/// </summary>
[Serializable, NetSerializable]
public sealed class PopupCursorEvent(string message, PopupType type, GameTick tick) : PopupEvent(message, type, tick)
{
    /// <summary>
    /// Creates a new prediction instance for this popup event.
    /// </summary>
    public readonly record struct PredictionInstance(string Message, PopupType Type, GameTick Tick) : IPopupPredictionInstance;
}

/// <summary>
/// Network event for displaying a popup at a world location.
/// </summary>
[Serializable, NetSerializable]
public sealed class PopupCoordinatesEvent(string message, PopupType type, GameTick tick, NetCoordinates coordinates, int predictionKey) : PopupEvent(message, type, tick)
{
    /// <summary>
    /// The coordinates where the popup should be displayed.
    /// </summary>
    public NetCoordinates Coordinates = coordinates;

    /// <summary>
    /// The key used to identify this popup event for prediction purposes.
    /// </summary>
    public int PredictionKey = predictionKey;

    /// <summary>
    /// Creates a new prediction instance for this popup event.
    /// </summary>
    /// <remarks>
    /// TODO: remove coords, as they are not used for prediction.
    /// </remarks>
    public readonly record struct PredictionInstance(string Message, PopupType Type, GameTick Tick, int PredictionKey) : IPopupPredictionInstance;
}

/// <summary>
/// Network event for displaying a popup above an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class PopupEntityEvent(string message, PopupType type, GameTick tick, NetEntity uid) : PopupEvent(message, type, tick)
{
    /// <summary>
    /// The entity above which the popup should be displayed.
    /// </summary>
    public NetEntity Uid = uid;

    /// <summary>
    /// Creates a new prediction instance for this popup event.
    /// </summary>
    public readonly record struct PredictionInstance(string Message, PopupType Type, GameTick Tick, NetEntity Uid) : IPopupPredictionInstance;
}

/// <summary>
/// Used to determine how a popup should appear visually to the client. Caution variants simply have a red color.
/// </summary>
/// <remarks>
/// Actions which can fail or succeed should use a smaller popup for failure and a larger popup for success.
/// Actions which have different popups for the user vs. others should use a larger popup for the user and a smaller popup for others.
/// Actions which result in harm or are otherwise dangerous should always show as the caution variant.
/// </remarks>
[Serializable, NetSerializable]
public enum PopupType : byte
{
    /// <summary>
    /// Small popups are the default, and denote actions that may be spammable or are otherwise unimportant.
    /// </summary>
    Small,
    SmallCaution,

    /// <summary>
    /// Medium popups should be used for actions which are not spammable but may not be particularly important.
    /// </summary>
    Medium,
    MediumCaution,

    /// <summary>
    /// Large popups should be used for actions which may be important or very important to one or more users,
    /// but is not life-threatening.
    /// </summary>
    Large,
    LargeCaution
}

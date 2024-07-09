using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Network;

namespace Content.Shared.Players;

/// <summary>
///     Content side for all data that tracks a player session.
///     Use <see cref="PlaIPlayerDatarver.Player.IPlayerData)"/> to retrieve this from an <see cref="PlayerData"/>.
///     <remarks>Not currently used on the client.</remarks>
/// </summary>
public sealed class ContentPlayerData
{
    /// <summary>
    ///     The session ID of the player owning this data.
    /// </summary>
    [ViewVariables]
    public NetUserId UserId { get; }

    /// <summary>
    ///     This is a backup copy of the player name stored on connection.
    ///     This is useful in the event the player disconnects.
    /// </summary>
    [ViewVariables]
    public string Name { get; }

    /// <summary>
    ///     The currently occupied mind of the player owning this data.
    ///     DO NOT DIRECTLY SET THIS UNLESS YOU KNOW WHAT YOU'RE DOING.
    /// </summary>
    [ViewVariables, Access(typeof(SharedMindSystem), typeof(SharedGameTicker))]
    public EntityUid? Mind { get; set; }

    /// <summary>
    ///     If true, the player is an admin and they explicitly de-adminned mid-game,
    ///     so they should not regain admin if they reconnect.
    /// </summary>
    public bool ExplicitlyDeadminned { get; set; }

    /// <summary>
    /// If true, the admin will not show up in adminwho except to admins with the <see cref="AdminFlags.Stealth"/> flag.
    /// </summary>
    public bool Stealthed { get; set; }

    /// <summary>
    /// Tracks how many messages in the player's message budget have been used. Goes up when a message is sent, goes
    /// down each rate-limiting interval. If this number is too high, messages cannot be sent.
    /// </summary>
    /// <example>
    /// Urist McSpammer says "I like cheese". This counter increases by one. If this counter reaches the
    /// message limit specified via the chat.rate_limit_count cvar, the player cannot send any more messages until
    /// enough time has passed that another attempt allows this value to decrement somewhat.
    /// </example>
    public int MessageRateOverTime;

    /// <summary>
    /// Tracks how many consumed characters in the player's character budget have been used. Goes up when a message is
    /// sent, goes down each rate-limiting interval. If this number is too high, messages cannot be sent.
    /// </summary>
    /// <example>
    /// Urist McSpammer says "I like cheese". This counter increases by the length of the message. If this
    /// counter reaches the message limit specified via the chat.max_announcement_length cvar, the player cannot send
    /// any more messages until enough time has passed that another attempt allows this value to decrement somewhat.
    /// </example>
    public int NetMessageLengthOverTime;

    /// <summary>
    /// The time that the current frame-of-reference for chat spam detection expires at. This is increased set by
    /// the value of the cvar chat.rate_limit_period when the first message is sent after the last period expired.
    /// </summary>
    public TimeSpan MessageCountExpiresAt;

    /// <summary>
    /// Whether rate limiting has been announced to the player.
    /// </summary>
    public bool RateLimitAnnouncedToPlayer;

    /// <summary>
    /// When an announcement to admins can next be sent at.
    /// </summary>
    public TimeSpan CanAnnounceToAdminsNextAt;

    public ContentPlayerData(NetUserId userId, string name)
    {
        UserId = userId;
        Name = name;
    }
}

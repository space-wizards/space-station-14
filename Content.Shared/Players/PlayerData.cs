using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Players;

/// <summary>
///     Content side for all data that tracks a player session.
///     Use <see cref="PlaIPlayerDatarver.Player.IPlayerData)"/> to retrieve this from an <see cref="IPlayerData"/>.
///     <remarks>Not currently used on the client.</remarks>
/// </summary>
public sealed class PlayerData
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

    public PlayerData(NetUserId userId, string name)
    {
        UserId = userId;
        Name = name;
    }
}


public static class PlayerDataExt
{
    /// <summary>
    ///     Gets the correctly cast instance of content player data from an engine player data storage.
    /// </summary>
    public static PlayerData? ContentData(this IPlayerData data)
    {
        return (PlayerData?) data.ContentDataUncast;
    }
}

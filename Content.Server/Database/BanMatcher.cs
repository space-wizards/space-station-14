﻿using System.Collections.Immutable;
using System.Net;
using Content.Server.IP;
using Robust.Shared.Network;

namespace Content.Server.Database;

/// <summary>
/// Implements logic to match a <see cref="ServerBanDef"/> against a player query.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is used by in-game ban matching code, and partially by the SQLite database layer.
/// Some logic is duplicated into both the SQLite and PostgreSQL database layers to provide more optimal SQL queries.
/// Both should be kept in sync, please!
/// </para>
/// </remarks>
public static class BanMatcher
{
    /// <summary>
    /// Check whether a ban matches the specified player info.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function does not check whether the ban itself is expired or manually unbanned.
    /// </para>
    /// </remarks>
    /// <param name="ban">The ban information.</param>
    /// <param name="player">Information about the player to match against.</param>
    /// <returns>True if the ban matches the provided player info.</returns>
    public static bool BanMatches(ServerBanDef ban, in PlayerInfo player)
    {
        var exemptFlags = player.ExemptFlags;
        // Any flag to bypass BlacklistedRange bans.
        if (exemptFlags != ServerBanExemptFlags.None)
            exemptFlags |= ServerBanExemptFlags.BlacklistedRange;

        if ((ban.ExemptFlags & exemptFlags) != 0)
            return false;

        if (!player.ExemptFlags.HasFlag(ServerBanExemptFlags.IP)
            && player.Address != null
            && ban.Address is not null
            && player.Address.IsInSubnet(ban.Address.Value)
            && (!ban.ExemptFlags.HasFlag(ServerBanExemptFlags.BlacklistedRange) || player.IsNewPlayer))
        {
            return true;
        }

        if (player.UserId is { } id && ban.UserId == id.UserId)
        {
            return true;
        }

        return player.HWId is { Length: > 0 } hwIdVar
               && ban.HWId != null
               && hwIdVar.AsSpan().SequenceEqual(ban.HWId.Value.AsSpan());
    }

    /// <summary>
    /// A simple struct containing player info used to match bans against.
    /// </summary>
    public struct PlayerInfo
    {
        /// <summary>
        /// The user ID of the player.
        /// </summary>
        public NetUserId? UserId;

        /// <summary>
        /// The IP address of the player.
        /// </summary>
        public IPAddress? Address;

        /// <summary>
        /// The hardware ID of the player.
        /// </summary>
        public ImmutableArray<byte>? HWId;

        /// <summary>
        /// Exemption flags the player has been granted.
        /// </summary>
        public ServerBanExemptFlags ExemptFlags;

        /// <summary>
        /// True if this player is new and is thus eligible for more bans.
        /// </summary>
        public bool IsNewPlayer;
    }
}

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Network;

namespace Content.Server.Connection;

public sealed partial class ConnectionManager
{
    private async Task<(ConnectionDenyReason, string, List<BanDef>? bansHit)?> CheckBanned(NetConnectingArgs e)
    {
        var addr = e.IP.Address;
        var userId = e.UserId;
        ImmutableArray<byte>? hwId = e.UserData.HWId;
        if (hwId.Value.Length == 0 || !_cfg.GetCVar(CCVars.BanHardwareIds))
        {
            // HWId not available for user's platform, don't look it up.
            // Or hardware ID checks disabled.
            hwId = null;
        }

        var modernHwid = e.UserData.ModernHWIds;

        if (modernHwid.Length == 0 && e.AuthType == LoginType.LoggedIn && _cfg.GetCVar(CCVars.RequireModernHardwareId))
        {
            return (ConnectionDenyReason.NoHwid, Loc.GetString("hwid-required"), null);
        }

        var bans = await _db.GetBansAsync(addr, userId, hwId, modernHwid, includeUnbanned: false);
        if (bans.Count > 0)
        {
            var firstBan = bans[0];
            var message = firstBan.FormatBanMessage(_cfg, _loc);
            return (ConnectionDenyReason.Ban, message, bans);
        }

        return null;
    }

    private async Task<(ConnectionDenyReason, string, List<BanDef>? bansHit)?> CheckPanicBunker(NetConnectingArgs e,
        Admin? adminData,
        bool panicBunkerEnabled)
    {
        // Player is admin or panic bunker is disabled.
        if (!panicBunkerEnabled || adminData != null)
        {
            return null;
        }

        var userId = e.UserId;
        var showReason = _cfg.GetCVar(CCVars.PanicBunkerShowReason);
        var customReason = _cfg.GetCVar(CCVars.PanicBunkerCustomReason);

        var minMinutesAge = _cfg.GetCVar(CCVars.PanicBunkerMinAccountAge);
        var record = await _db.GetPlayerRecordByUserId(userId);
        var validAccountAge = record != null &&
                              record.FirstSeenTime.CompareTo(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(minMinutesAge)) <= 0;
        var bypassAllowed = _cfg.GetCVar(CCVars.BypassBunkerWhitelist) && await _db.GetWhitelistStatusAsync(userId);

        // Use the custom reason if it exists & they don't have the minimum account age
        if (customReason != string.Empty && !validAccountAge && !bypassAllowed)
        {
            return (ConnectionDenyReason.Panic, customReason, null);
        }

        if (showReason && !validAccountAge && !bypassAllowed)
        {
            return (ConnectionDenyReason.Panic,
                Loc.GetString("panic-bunker-account-denied-reason",
                    ("reason", Loc.GetString("panic-bunker-account-reason-account", ("minutes", minMinutesAge)))), null);
        }

        var minOverallMinutes = _cfg.GetCVar(CCVars.PanicBunkerMinOverallMinutes);
        var overallTime = ( await _db.GetPlayTimes(e.UserId)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
        var haveMinOverallTime = overallTime != null && overallTime.TimeSpent.TotalMinutes > minOverallMinutes;

        // Use the custom reason if it exists & they don't have the minimum time
        if (customReason != string.Empty && !haveMinOverallTime && !bypassAllowed)
        {
            return (ConnectionDenyReason.Panic, customReason, null);
        }

        if (showReason && !haveMinOverallTime && !bypassAllowed)
        {
            return (ConnectionDenyReason.Panic,
                Loc.GetString("panic-bunker-account-denied-reason",
                    ("reason", Loc.GetString("panic-bunker-account-reason-overall", ("minutes", minOverallMinutes)))), null);
        }

        if (!validAccountAge || !haveMinOverallTime && !bypassAllowed)
        {
            return (ConnectionDenyReason.Panic, Loc.GetString("panic-bunker-account-denied"), null);
        }

        return null;
    }

    private async Task<(ConnectionDenyReason, string, List<BanDef>? bansHit)?> CheckPlayerCount(NetConnectingArgs e,
        Admin? adminData,
        int softPlayerCount)
    {
        var userId = e.UserId;
        _ticker ??= _entityManager.SystemOrNull<GameTicker>();

        var wasInGame = _ticker != null &&
                        _ticker.PlayerGameStatuses.TryGetValue(userId, out var status) &&
                        status == PlayerGameStatus.JoinedGame;
        var adminBypass = _cfg.GetCVar(CCVars.AdminBypassMaxPlayers) && adminData != null;

        if (!_cfg.GetCVar(CCVars.AdminsCountForMaxPlayers))
        {
            softPlayerCount -= _adminManager.ActiveAdmins.Count();
        }

        if (softPlayerCount >= _cfg.GetCVar(CCVars.SoftMaxPlayers) && !adminBypass && !wasInGame)
        {
            return (ConnectionDenyReason.Full, Loc.GetString("soft-player-cap-full"), null);
        }
        return null;
    }

    private async Task<(ConnectionDenyReason, string, List<BanDef>? bansHit)?> CheckWhitelist(NetConnectingArgs e,
        Admin? adminData,
        int softPlayerCount,
        bool whitelistEnabled)
    {
        // Player is an admin or whitelist is turned off.
        if (!whitelistEnabled || adminData != null)
        {
            return null;
        }

        if (_whitelists is null)
        {
            _sawmill.Error("Whitelist enabled but no whitelists loaded.");
            // Misconfigured, deny everyone.
            return (ConnectionDenyReason.Whitelist, Loc.GetString("generic-misconfigured"), null);
        }

        foreach (var whitelist in _whitelists)
        {
            if (!IsValid(whitelist, softPlayerCount))
            {
                // Not valid for current player count.
                continue;
            }

            var whitelistStatus = await IsWhitelisted(whitelist, e.UserData, _sawmill);
            if (!whitelistStatus.isWhitelisted)
            {
                // Not whitelisted.
                return (ConnectionDenyReason.Whitelist, Loc.GetString("whitelist-fail-prefix", ("msg", whitelistStatus.denyMessage!)), null);
            }

            // Whitelisted, don't check any more.
            break;
        }

        return null;
    }

    private async Task<(ConnectionDenyReason, string, List<BanDef>? bansHit)?> CheckVpn(NetConnectingArgs e,
        Admin? adminData,
        bool ipIntelEnabled)
    {
        if (adminData != null || !ipIntelEnabled)
        {
            return null;
        }

        var result = await _ipintel.IsVpnOrProxy(e);

        if (result.IsBad)
        {
            return (ConnectionDenyReason.IPChecks, result.Reason, null);
        }

        return null;
    }
}

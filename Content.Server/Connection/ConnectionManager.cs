using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;

/*
 * TODO: Remove baby jail code once a more mature gateway process is established. This code is only being issued as a stopgap to help with potential tiding in the immediate future.
 */

namespace Content.Server.Connection
{
    public interface IConnectionManager
    {
        void Initialize();
        void PostInit();

        /// <summary>
        /// Temporarily allow a user to bypass regular connection requirements.
        /// </summary>
        /// <remarks>
        /// The specified user will be allowed to bypass regular player cap,
        /// whitelist and panic bunker restrictions for <paramref name="duration"/>.
        /// Bans are not bypassed.
        /// </remarks>
        /// <param name="user">The user to give a temporary bypass.</param>
        /// <param name="duration">How long the bypass should last for.</param>
        void AddTemporaryConnectBypass(NetUserId user, TimeSpan duration);
    }

    /// <summary>
    ///     Handles various duties like guest username assignment, bans, connection logs, etc...
    /// </summary>
    public sealed partial class ConnectionManager : IConnectionManager
    {
        [Dependency] private readonly IPlayerManager _plyMgr = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        private ISawmill _sawmill = default!;
        private readonly Dictionary<NetUserId, TimeSpan> _temporaryBypasses = [];


        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("connections");

            _netMgr.Connecting += NetMgrOnConnecting;
            _netMgr.AssignUserIdCallback = AssignUserIdCallback;
            _plyMgr.PlayerStatusChanged += PlayerStatusChanged;
            // Approval-based IP bans disabled because they don't play well with Happy Eyeballs.
            // _netMgr.HandleApprovalCallback = HandleApproval;
        }

        public void AddTemporaryConnectBypass(NetUserId user, TimeSpan duration)
        {
            ref var time = ref CollectionsMarshal.GetValueRefOrAddDefault(_temporaryBypasses, user, out _);
            var newTime = _gameTiming.RealTime + duration;
            // Make sure we only update the time if we wouldn't shrink it.
            if (newTime > time)
                time = newTime;
        }

        /*
        private async Task<NetApproval> HandleApproval(NetApprovalEventArgs eventArgs)
        {
            var ban = await _db.GetServerBanByIpAsync(eventArgs.Connection.RemoteEndPoint.Address);
            if (ban != null)
            {
                var expires = Loc.GetString("ban-banned-permanent");
                if (ban.ExpirationTime is { } expireTime)
                {
                    var duration = expireTime - ban.BanTime;
                    var utc = expireTime.ToUniversalTime();
                    expires = Loc.GetString("ban-expires", ("duration", duration.TotalMinutes.ToString("N0")), ("time", utc.ToString("f")));
                }
                var reason = Loc.GetString("ban-banned-1") + "\n" + Loc.GetString("ban-banned-2", ("reason", this.Reason)) + "\n" + expires;;
                return NetApproval.Deny(reason);
            }

            return NetApproval.Allow();
        }
        */

        private async Task NetMgrOnConnecting(NetConnectingArgs e)
        {
            var deny = await ShouldDeny(e);

            var addr = e.IP.Address;
            var userId = e.UserId;

            var serverId = (await _serverDbEntry.ServerEntity).Id;

            if (deny != null)
            {
                var (reason, msg, banHits) = deny.Value;

                var id = await _db.AddConnectionLogAsync(userId, e.UserName, addr, e.UserData.HWId, reason, serverId);
                if (banHits is { Count: > 0 })
                    await _db.AddServerBanHitsAsync(id, banHits);

                var properties = new Dictionary<string, object>();
                if (reason == ConnectionDenyReason.Full)
                    properties["delay"] = _cfg.GetCVar(CCVars.GameServerFullReconnectDelay);

                e.Deny(new NetDenyReason(msg, properties));
            }
            else
            {
                await _db.AddConnectionLogAsync(userId, e.UserName, addr, e.UserData.HWId, null, serverId);

                if (!ServerPreferencesManager.ShouldStorePrefs(e.AuthType))
                    return;

                await _db.UpdatePlayerRecordAsync(userId, e.UserName, addr, e.UserData.HWId);
            }
        }

        private async void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.Connected)
            {
                AdminAlertIfSharedConnection(args.Session);
            }
        }

        private void AdminAlertIfSharedConnection(ICommonSession newSession)
        {
            var playerThreshold = _cfg.GetCVar(CCVars.AdminAlertMinPlayersSharingConnection);
            if (playerThreshold < 0)
                return;

            var addr = newSession.Channel.RemoteEndPoint.Address;

            var otherConnectionsFromAddress = _plyMgr.Sessions.Where(session =>
                    session.Status is SessionStatus.Connected or SessionStatus.InGame
                    && session.Channel.RemoteEndPoint.Address.Equals(addr)
                    && session.UserId != newSession.UserId)
                .ToList();

            var otherConnectionCount = otherConnectionsFromAddress.Count;
            if (otherConnectionCount + 1 < playerThreshold) // Add one for the total, not just others, using the address
                return;

            var username = newSession.Name;
            var otherUsernames = string.Join(", ",
                otherConnectionsFromAddress.Select(session => session.Name));

            _chatManager.SendAdminAlert(Loc.GetString("admin-alert-shared-connection",
                ("player", username),
                ("otherCount", otherConnectionCount),
                ("otherList", otherUsernames)));
        }

        /*
         * TODO: Jesus H Christ what is this utter mess of a function
         * TODO: Break this apart into is constituent steps.
         */
        private async Task<(ConnectionDenyReason, string, List<ServerBanDef>? bansHit)?> ShouldDeny(
            NetConnectingArgs e)
        {
            // Check if banned.
            var addr = e.IP.Address;
            var userId = e.UserId;
            ImmutableArray<byte>? hwId = e.UserData.HWId;
            if (hwId.Value.Length == 0 || !_cfg.GetCVar(CCVars.BanHardwareIds))
            {
                // HWId not available for user's platform, don't look it up.
                // Or hardware ID checks disabled.
                hwId = null;
            }

            var bans = await _db.GetServerBansAsync(addr, userId, hwId, includeUnbanned: false);
            if (bans.Count > 0)
            {
                var firstBan = bans[0];
                var message = firstBan.FormatBanMessage(_cfg, _loc);
                return (ConnectionDenyReason.Ban, message, bans);
            }

            if (HasTemporaryBypass(userId))
            {
                _sawmill.Verbose("User {UserId} has temporary bypass, skipping further connection checks", userId);
                return null;
            }

            var adminData = await _db.GetAdminDataForAsync(e.UserId);

            if (_cfg.GetCVar(CCVars.PanicBunkerEnabled) && adminData == null)
            {
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
            }

            if (_cfg.GetCVar(CCVars.BabyJailEnabled) && adminData == null)
            {
                var result = await IsInvalidConnectionDueToBabyJail(userId, e);

                if (result.IsInvalid)
                    return (ConnectionDenyReason.BabyJail, result.Reason, null);
            }

            var wasInGame = EntitySystem.TryGet<GameTicker>(out var ticker) &&
                            ticker.PlayerGameStatuses.TryGetValue(userId, out var status) &&
                            status == PlayerGameStatus.JoinedGame;
            var adminBypass = _cfg.GetCVar(CCVars.AdminBypassMaxPlayers) && adminData != null;
            if ((_plyMgr.PlayerCount >= _cfg.GetCVar(CCVars.SoftMaxPlayers) && !adminBypass) && !wasInGame)
            {
                return (ConnectionDenyReason.Full, Loc.GetString("soft-player-cap-full"), null);
            }

            // Checks for whitelist IF it's enabled AND the user isn't an admin. Admins are always allowed.
            if (_cfg.GetCVar(CCVars.WhitelistEnabled) && adminData is null)
            {
                if (_whitelists is null)
                {
                    _sawmill.Error("Whitelist enabled but no whitelists loaded.");
                    // Misconfigured, deny everyone.
                    return (ConnectionDenyReason.Whitelist, Loc.GetString("whitelist-misconfigured"), null);
                }

                foreach (var whitelist in _whitelists)
                {
                    if (!IsValid(whitelist, _plyMgr.PlayerCount))
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
            }

            return null;
        }

        private async Task<(bool IsInvalid, string Reason)> IsInvalidConnectionDueToBabyJail(NetUserId userId, NetConnectingArgs e)
        {
            // If you're whitelisted then bypass this whole thing
            if (await _db.GetWhitelistStatusAsync(userId))
                return (false, "");

            // Initial cvar retrieval
            var showReason = _cfg.GetCVar(CCVars.BabyJailShowReason);
            var reason = _cfg.GetCVar(CCVars.BabyJailCustomReason);
            var maxAccountAgeMinutes = _cfg.GetCVar(CCVars.BabyJailMaxAccountAge);
            var maxPlaytimeMinutes = _cfg.GetCVar(CCVars.BabyJailMaxOverallMinutes);

            // Wait some time to lookup data
            var record = await _db.GetPlayerRecordByUserId(userId);

            // No player record = new account or the DB is having a skill issue
            if (record == null)
                return (false, "");

            var isAccountAgeInvalid = record.FirstSeenTime.CompareTo(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(maxAccountAgeMinutes)) <= 0;

            if (isAccountAgeInvalid)
            {
                _sawmill.Debug($"Baby jail will deny {userId} for account age {record.FirstSeenTime}"); // Remove on or after 2024-09
            }

            if (isAccountAgeInvalid && showReason)
            {
                var locAccountReason = reason != string.Empty
                    ? reason
                    : Loc.GetString("baby-jail-account-denied-reason",
                        ("reason",
                            Loc.GetString(
                                "baby-jail-account-reason-account",
                                ("minutes", maxAccountAgeMinutes))));

                return (true, locAccountReason);
            }

            var overallTime = ( await _db.GetPlayTimes(e.UserId)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
            var isTotalPlaytimeInvalid = overallTime != null && overallTime.TimeSpent.TotalMinutes >= maxPlaytimeMinutes;

            if (isTotalPlaytimeInvalid)
            {
                _sawmill.Debug($"Baby jail will deny {userId} for playtime {overallTime!.TimeSpent}"); // Remove on or after 2024-09
            }

            if (isTotalPlaytimeInvalid && showReason)
            {
                var locPlaytimeReason = reason != string.Empty
                    ? reason
                    : Loc.GetString("baby-jail-account-denied-reason",
                        ("reason",
                            Loc.GetString(
                                "baby-jail-account-reason-overall",
                                ("minutes", maxPlaytimeMinutes))));

                return (true, locPlaytimeReason);
            }

            if (!showReason && isTotalPlaytimeInvalid || isAccountAgeInvalid)
                return (true, Loc.GetString("baby-jail-account-denied"));

            return (false, "");
        }

        private bool HasTemporaryBypass(NetUserId user)
        {
            return _temporaryBypasses.TryGetValue(user, out var time) && time > _gameTiming.RealTime;
        }

        private async Task<NetUserId?> AssignUserIdCallback(string name)
        {
            if (!_cfg.GetCVar(CCVars.GamePersistGuests))
            {
                return null;
            }

            var userId = await _db.GetAssignedUserIdAsync(name);
            if (userId != null)
            {
                return userId;
            }

            var assigned = new NetUserId(Guid.NewGuid());
            await _db.AssignUserIdAsync(name, assigned);
            return assigned;
        }
    }
}

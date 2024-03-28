using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Connection.Whitelist;
using Content.Server.Connection.Whitelist.Conditions;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;


namespace Content.Server.Connection
{
    public interface IConnectionManager
    {
        void Initialize();
        void PostInit();
    }

    /// <summary>
    ///     Handles various duties like guest username assignment, bans, connection logs, etc...
    /// </summary>
    public sealed class ConnectionManager : IConnectionManager
    {
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IPlayerManager _plyMgr = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private ISawmill _sawmill = default!;
        private PlayerConnectionWhitelistPrototype[]? _whitelists;
        private readonly Dictionary<NetUserId, List<IAdminRemarksRecord>> _adminRemarksCache = new();

        public void Initialize()
        {
            _netMgr.Connecting += NetMgrOnConnecting;
            _netMgr.AssignUserIdCallback = AssignUserIdCallback;
            _sawmill = Logger.GetSawmill("connmgr");
            // Approval-based IP bans disabled because they don't play well with Happy Eyeballs.
            // _netMgr.HandleApprovalCallback = HandleApproval;
        }

        private void UpdateWhitelists(string s)
        {
            var list = new List<PlayerConnectionWhitelistPrototype>();
            var allWhitelists = _prototypeManager.EnumeratePrototypes<PlayerConnectionWhitelistPrototype>().ToList();
            foreach (var id in s.Split(','))
            {
                if (allWhitelists.FirstOrDefault(p => p.ID == id) is { } prototype)
                {
                    list.Add(prototype);
                }
                else
                {
                    _sawmill.Error($"Whitelist prototype {id} does not exist.");
                    _whitelists = Array.Empty<PlayerConnectionWhitelistPrototype>();
                    return;
                }
            }

            _whitelists = list.ToArray();
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

            var adminData = await _dbManager.GetAdminDataForAsync(e.UserId);

            if (_cfg.GetCVar(CCVars.PanicBunkerEnabled) && adminData == null)
            {
                var showReason = _cfg.GetCVar(CCVars.PanicBunkerShowReason);
                var customReason = _cfg.GetCVar(CCVars.PanicBunkerCustomReason);

                var minMinutesAge = _cfg.GetCVar(CCVars.PanicBunkerMinAccountAge);
                var record = await _dbManager.GetPlayerRecordByUserId(userId);
                var validAccountAge = record != null &&
                                      record.FirstSeenTime.CompareTo(DateTimeOffset.Now - TimeSpan.FromMinutes(minMinutesAge)) <= 0;
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

                var minOverallHours = _cfg.GetCVar(CCVars.PanicBunkerMinOverallHours);
                var overallTime = ( await _db.GetPlayTimes(e.UserId)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
                var haveMinOverallTime = overallTime != null && overallTime.TimeSpent.TotalHours > minOverallHours;

                // Use the custom reason if it exists & they don't have the minimum time
                if (customReason != string.Empty && !haveMinOverallTime && !bypassAllowed)
                {
                    return (ConnectionDenyReason.Panic, customReason, null);
                }

                if (showReason && !haveMinOverallTime && !bypassAllowed)
                {
                    return (ConnectionDenyReason.Panic,
                        Loc.GetString("panic-bunker-account-denied-reason",
                            ("reason", Loc.GetString("panic-bunker-account-reason-overall", ("hours", minOverallHours)))), null);
                }

                if (!validAccountAge || !haveMinOverallTime && !bypassAllowed)
                {
                    return (ConnectionDenyReason.Panic, Loc.GetString("panic-bunker-account-denied"), null);
                }
            }

            var wasInGame = EntitySystem.TryGet<GameTicker>(out var ticker) &&
                            ticker.PlayerGameStatuses.TryGetValue(userId, out var status) &&
                            status == PlayerGameStatus.JoinedGame;
            var adminBypass = _cfg.GetCVar(CCVars.AdminBypassMaxPlayers) && adminData != null;
            if ((_plyMgr.PlayerCount >= _cfg.GetCVar(CCVars.SoftMaxPlayers) && !adminBypass) && !wasInGame)
            {
                return (ConnectionDenyReason.Full, Loc.GetString("soft-player-cap-full"), null);
            }

            var bans = await _db.GetServerBansAsync(addr, userId, hwId, includeUnbanned: false);
            if (bans.Count > 0)
            {
                var firstBan = bans[0];
                var message = firstBan.FormatBanMessage(_cfg, _loc);
                return (ConnectionDenyReason.Ban, message, bans);
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

        public bool IsValid(PlayerConnectionWhitelistPrototype whitelist, int playerCount)
        {
            return playerCount >= whitelist.MinimumPlayers && playerCount <= whitelist.MaximumPlayers;
        }

        public async Task<(bool isWhitelisted, string? denyMessage)> IsWhitelisted(PlayerConnectionWhitelistPrototype whitelist, NetUserData data, ISawmill sawmill)
        {
            foreach (var condition in whitelist.Conditions)
            {
                var matched = false;
                string denyMessage;
                switch (condition.GetType())
                {
                    case { } t when t == typeof(ConditionAlwaysMatch):
                        matched = true;
                        denyMessage = Loc.GetString("whitelist-always-deny");
                        break;
                    case { } t when t == typeof(ConditionManualWhitelist):
                        matched = !(await _db.GetWhitelistStatusAsync(data.UserId));
                        denyMessage = Loc.GetString("whitelist-manual");
                        break;
                    case { } t when t == typeof(ConditionManualBlacklist):
                        var blacklisted = await _db.GetBlacklistStatusAsync(data.UserId);
                        matched = blacklisted;
                        denyMessage = Loc.GetString("whitelist-blacklisted");
                        break;
                    case { } t when t == typeof(ConditionNotesDateRange):
                        var conditionNotes = (ConditionNotesDateRange)condition;
                        var remarks = await GetAdminRemarks(data.UserId);
                        remarks = remarks.Where(x => x.CreatedAt > DateTime.Now.AddDays(-conditionNotes.Range)).ToList();
                        if (!conditionNotes.IncludeExpired)
                            // If we're not including expired notes, filter them out.
                            remarks = remarks.Where(x => x.ExpirationTime is null || x.ExpirationTime > DateTime.Now).ToList();
                        var remarksCopy = remarks.ToList();
                        foreach (var adminRemarksRecord in remarks)
                        {
                            // In order to get the severity of the remark, we need to see if its a AdminNoteRecord.
                            if (adminRemarksRecord is not AdminNoteRecord adminNoteRecord)
                                continue;

                            // We want to filter out secret notes if we're not including them.
                            if (!conditionNotes.IncludeSecret && adminNoteRecord.Secret)
                            {
                                remarksCopy.Remove(adminRemarksRecord);
                                continue;
                            }

                            // At this point, we need to remove the note if it's not within the severity range.
                            if (adminNoteRecord.Severity < conditionNotes.MinimumSeverity)
                            {
                                remarksCopy.Remove(adminRemarksRecord);
                            }
                        }

                        matched = remarksCopy.Count > 0;
                        denyMessage = Loc.GetString("whitelist-notes");
                        break;
                    case { } t when t == typeof(ConditionPlayerCount):
                        var conditionPlayerCount = (ConditionPlayerCount)condition;
                        var count = _plyMgr.PlayerCount;
                        // Match if the player count is within the range.
                        matched = count >= conditionPlayerCount.MinimumPlayers && count <= conditionPlayerCount.MaximumPlayers;
                        denyMessage = Loc.GetString("whitelist-player-count");
                        break;
                    case { } t when t == typeof(ConditionPlaytime):
                        var conditionPlaytime = (ConditionPlaytime)condition;
                        var playtime = await _db.GetPlayTimes(data.UserId);
                        var tracker = playtime.Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
                        if (tracker is null)
                        {
                            matched = false;
                        }
                        else
                        {
                            matched = tracker.TimeSpent.TotalMinutes < conditionPlaytime.MinimumPlaytime;
                        }
                        denyMessage = Loc.GetString("whitelist-playtime", ("minutes", conditionPlaytime.MinimumPlaytime));
                        break;
                    case { } t when t == typeof(ConditionNotesPlaytimeRange):
                        var conditionNotesPlaytimeRange = (ConditionNotesPlaytimeRange)condition;
                        var remarksPlaytimeRange = await GetAdminRemarks(data.UserId);
                        // In order to filter by playtime, we need to do the following: playtimeAtNote >= overallPlaytime - Range
                        var overallPlaytime = await _db.GetPlayTimes(data.UserId);
                        var overallTracker = overallPlaytime.Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall);
                        if (overallTracker is null)
                        {
                            matched = false;
                            denyMessage = Loc.GetString("whitelist-notes");
                            break;
                        }
                        remarksPlaytimeRange = remarksPlaytimeRange.Where(x =>
                            x.PlaytimeAtNote >= overallTracker.TimeSpent - TimeSpan.FromMinutes(conditionNotesPlaytimeRange.Range)).ToList();
                        if (!conditionNotesPlaytimeRange.IncludeExpired)
                            remarksPlaytimeRange = remarksPlaytimeRange.Where(x => x.ExpirationTime is null || x.ExpirationTime > DateTime.Now).ToList();
                        var copy = remarksPlaytimeRange.ToList();
                        foreach (var adminRemarksRecord in remarksPlaytimeRange)
                        {
                            // In order to get the severity of the remark, we need to see if its a AdminNoteRecord.
                            if (adminRemarksRecord is not AdminNoteRecord adminNoteRecord)
                                continue;

                            // We want to filter out secret notes if we're not including them.
                            if (!conditionNotesPlaytimeRange.IncludeSecret && adminNoteRecord.Secret)
                            {
                                copy.Remove(adminRemarksRecord);
                                continue;
                            }

                            // At this point, we need to remove the note if it's not within the severity range.
                            if (adminNoteRecord.Severity < conditionNotesPlaytimeRange.MinimumSeverity)
                            {
                                copy.Remove(adminRemarksRecord);
                                continue;
                            }
                        }

                        matched = copy.Count > 0;
                        denyMessage = Loc.GetString("whitelist-notes");
                        break;
                    default:
                        throw new NotImplementedException($"Whitelist condition {condition.GetType().Name} not implemented");
                }

                sawmill.Verbose($"User {data.UserName} whitelist condition {condition.GetType().Name} result: {matched}");
                sawmill.Verbose($"Action: {condition.Action.ToString()}");

                switch (condition.Action)
                {
                    case ConditionAction.Allow:
                        if (matched)
                        {
                            sawmill.Verbose($"User {data.UserName} passed whitelist condition {condition.GetType().Name} and it's a breaking condition");
                            return (true, denyMessage);
                        }
                        break;
                    case ConditionAction.Deny:
                        if (matched)
                        {
                            sawmill.Verbose($"User {data.UserName} failed whitelist condition {condition.GetType().Name}");
                            return (false, denyMessage);
                        }
                        break;
                    default:
                        sawmill.Verbose($"User {data.UserName} failed whitelist condition {condition.GetType().Name} but it's not a breaking condition");
                        break;
                }
            }

            sawmill.Verbose($"User {data.UserName} passed all whitelist conditions");
            return (true, null);
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

        private async Task<List<IAdminRemarksRecord>> GetAdminRemarks(NetUserId userId)
        {
            if (_adminRemarksCache.TryGetValue(userId, out var remarks))
            {
                return remarks;
            }

            var records = await _db.GetAllAdminRemarks(userId.UserId);
            _adminRemarksCache.Add(userId, records);
            return records;
            // I could make the remarks in the cache expire after a certain amount of time, but the usecases of where you would want them to expire are low.
            // Rounds take like a max of 2/2.5 hours. So, it's not worth it.
        }

        public void PostInit()
        {
            _cfg.OnValueChanged(CCVars.WhitelistPrototypeList, UpdateWhitelists, true);
        }
    }
}

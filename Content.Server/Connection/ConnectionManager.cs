using System.Collections.Immutable;
using System.Threading.Tasks;
using Content.Server.Corvax.Sponsors;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;


namespace Content.Server.Connection
{
    public interface IConnectionManager
    {
        void Initialize();
        Task<bool> HavePrivilegedJoin(NetUserId userId); // Corvax-Queue
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
        [Dependency] private readonly ServerSponsorsManager _sponsorsManager = default!; // Corvax-Sponsors

        public void Initialize()
        {
            _netMgr.Connecting += NetMgrOnConnecting;
            _netMgr.AssignUserIdCallback = AssignUserIdCallback;
            // Approval-based IP bans disabled because they don't play well with Happy Eyeballs.
            // _netMgr.HandleApprovalCallback = HandleApproval;
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

            if (deny != null)
            {
                var (reason, msg, banHits) = deny.Value;

                var id = await _db.AddConnectionLogAsync(userId, e.UserName, addr, e.UserData.HWId, reason);
                if (banHits is { Count: > 0 })
                    await _db.AddServerBanHitsAsync(id, banHits);

                e.Deny(msg);
            }
            else
            {
                await _db.AddConnectionLogAsync(userId, e.UserName, addr, e.UserData.HWId, null);

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

            if (_cfg.GetCVar(CCVars.PanicBunkerEnabled))
            {
                var record = await _dbManager.GetPlayerRecordByUserId(userId);

                if ((record is null ||
                    (record.FirstSeenTime.CompareTo(DateTimeOffset.Now - TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.PanicBunkerMinAccountAge))) > 0)))
                {
                    return (ConnectionDenyReason.Panic, Loc.GetString("panic-bunker-account-denied"), null);
                }
            }

            // Corvax-Queue-Start
            var isPrivileged = await HavePrivilegedJoin(e.UserId);
            var isQueueEnabled = _cfg.GetCVar(CCVars.QueueEnabled);
            if (_plyMgr.PlayerCount >= _cfg.GetCVar(CCVars.SoftMaxPlayers) && !isPrivileged && !isQueueEnabled)
            // Corvax-Queue-End
            {
                return (ConnectionDenyReason.Full, Loc.GetString("soft-player-cap-full"), null);
            }

            var bans = await _db.GetServerBansAsync(addr, userId, hwId, includeUnbanned: false);
            if (bans.Count > 0)
            {
                var firstBan = bans[0];
                return (ConnectionDenyReason.Ban, firstBan.DisconnectMessage, bans);
            }

            if (_cfg.GetCVar(CCVars.WhitelistEnabled)
                && await _db.GetWhitelistStatusAsync(userId) == false
                && adminData is null)
            {
                return (ConnectionDenyReason.Whitelist, Loc.GetString(_cfg.GetCVar(CCVars.WhitelistReason)), null);
            }

            return null;
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

        // Corvax-Queue-Start: Make these conditions in one place, for checks in the connection and in the queue
        public async Task<bool> HavePrivilegedJoin(NetUserId userId)
        {
            var adminData = await _dbManager.GetAdminDataForAsync(userId);
            var sponsorData = _sponsorsManager.GetSponsorInfo(userId); // Corvax-Sponsors
            
            var havePriorityJoin = sponsorData?.HavePriorityJoin == true; // Corvax-Sponsors
            var wasInGame = EntitySystem.TryGet<GameTicker>(out var ticker) &&
                            ticker.PlayerGameStatuses.TryGetValue(userId, out var status) &&
                            status == PlayerGameStatus.JoinedGame;
            return adminData != null ||
                   havePriorityJoin || // Corvax-Sponsors
                   wasInGame;
        }
        // Corvax-Queue-End
    }
}

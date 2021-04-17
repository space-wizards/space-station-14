using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Preferences;
using Content.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;

#nullable enable

namespace Content.Server
{
    public interface IConnectionManager
    {
        void Initialize();
    }

    /// <summary>
    ///     Handles various duties like guest username assignment, bans, connection logs, etc...
    /// </summary>
    public sealed class ConnectionManager : IConnectionManager
    {
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

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
                var expires = "This is a permanent ban.";
                if (ban.ExpirationTime is { } expireTime)
                {
                    var duration = expireTime - ban.BanTime;
                    var utc = expireTime.ToUniversalTime();
                    expires = $"This ban is for {duration.TotalMinutes} minutes and will expire at {utc:f} UTC.";
                }
                var reason = $@"You, or another user of this computer or connection is banned from playing here.
The ban reason is: ""{ban.Reason}""
{expires}";
                return NetApproval.Deny(reason);
            }

            return NetApproval.Allow();
        }
        */

        private async Task NetMgrOnConnecting(NetConnectingArgs e)
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

            var ban = await _db.GetServerBanAsync(addr, userId, hwId);
            if (ban != null)
            {
                var expires = "This is a permanent ban.";
                if (ban.ExpirationTime is { } expireTime)
                {
                    var duration = expireTime - ban.BanTime;
                    var utc = expireTime.ToUniversalTime();
                    expires = $"This ban is for {duration.TotalMinutes:N0} minutes and will expire at {utc:f} UTC.";
                }
                var reason = $@"You, or another user of this computer or connection, are banned from playing here.
The ban reason is: ""{ban.Reason}""
{expires}";
                e.Deny(reason);
                return;
            }

            if (!ServerPreferencesManager.ShouldStorePrefs(e.AuthType))
            {
                return;
            }

            await _db.UpdatePlayerRecordAsync(userId, e.UserName, addr, e.UserData.HWId);
            await _db.AddConnectionLogAsync(userId, e.UserName, addr, e.UserData.HWId);
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

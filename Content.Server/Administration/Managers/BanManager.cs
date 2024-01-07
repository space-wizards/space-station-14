using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Managers;

public sealed class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
    public const string JobPrefix = "Job:";

    private readonly Dictionary<NetUserId, HashSet<ServerRoleBanDef>> _cachedRoleBans = new();

    public void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        _netManager.RegisterNetMessage<MsgRoleBans>();
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected || _cachedRoleBans.ContainsKey(e.Session.UserId))
            return;

        var netChannel = e.Session.ConnectedClient;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        await CacheDbRoleBans(e.Session.UserId, netChannel.RemoteEndPoint.Address, hwId);

        SendRoleBans(e.Session);
    }

    private async Task<bool> AddRoleBan(ServerRoleBanDef banDef)
    {
        banDef = await _db.AddServerRoleBanAsync(banDef);

        if (banDef.UserId != null)
        {
            _cachedRoleBans.GetOrNew(banDef.UserId.Value).Add(banDef);
        }

        return true;
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
    {
        return _cachedRoleBans.TryGetValue(playerUserId, out var roleBans) ? roleBans.Select(banDef => banDef.Role).ToHashSet() : null;
    }

    private async Task CacheDbRoleBans(NetUserId userId, IPAddress? address = null, ImmutableArray<byte>? hwId = null)
    {
        var roleBans = await _db.GetServerRoleBansAsync(address, userId, hwId, false);

        var userRoleBans = new HashSet<ServerRoleBanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        _cachedRoleBans[userId] = userRoleBans;
    }

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new List<NetUserId>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (!_playerManager.TryGetSessionById(player, out _))
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveWhere(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
    }

    #region Server Bans
    public async void CreateServerBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, uint? minutes, NoteSeverity severity, string reason)
    {
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        _systems.TryGetEntitySystem<GameTicker>(out var ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerBanDef(
            null,
            target,
            addressRange,
            hwid,
            DateTimeOffset.Now,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null);

        await _db.AddServerBanAsync(banDef);
        var adminName = banningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");
        var targetName = target is null ? "null" : $"{targetUsername} ({target})";
        var addressRangeString = addressRange != null
            ? $"{addressRange.Value.Item1}/{addressRange.Value.Item2}"
            : "null";
        var hwidString = hwid != null
            ? string.Concat(hwid.Value.Select(x => x.ToString("x2")))
            : "null";
        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        // If we're not banning a player we don't care about disconnecting people
        if (target == null)
            return;

        // Is the player connected?
        if (!_playerManager.TryGetSessionById(target.Value, out var targetPlayer))
            return;
        // If they are, kick them
        var message = banDef.FormatBanMessage(_cfg, _localizationManager);
        targetPlayer.ConnectedClient.Disconnect(message);
    }
    #endregion

    #region Job Bans
    // If you are trying to remove timeOfBan, please don't. It's there because the note system groups role bans by time, reason and banning admin.
    // Removing it will clutter the note list. Please also make sure that department bans are applied to roles with the same DateTimeOffset.
    public async void CreateRoleBan(NetUserId? target, string? targetUsername, NetUserId? banningAdmin, (IPAddress, int)? addressRange, ImmutableArray<byte>? hwid, string role, uint? minutes, NoteSeverity severity, string reason, DateTimeOffset timeOfBan)
    {
        if (!_prototypeManager.TryIndex(role, out JobPrototype? _))
        {
            throw new ArgumentException($"Invalid role '{role}'", nameof(role));
        }

        role = string.Concat(JobPrefix, role);
        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes.Value);
        }

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var playtime = target == null ? TimeSpan.Zero : (await _db.GetPlayTimes(target.Value)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;

        var banDef = new ServerRoleBanDef(
            null,
            target,
            addressRange,
            hwid,
            timeOfBan,
            expires,
            roundId,
            playtime,
            reason,
            severity,
            banningAdmin,
            null,
            role);

        if (!await AddRoleBan(banDef))
        {
            _chat.SendAdminAlert(Loc.GetString("cmd-roleban-existing", ("target", targetUsername ?? "null"), ("role", role)));
            return;
        }

        var length = expires == null ? Loc.GetString("cmd-roleban-inf") : Loc.GetString("cmd-roleban-until", ("expires", expires));
        _chat.SendAdminAlert(Loc.GetString("cmd-roleban-success", ("target", targetUsername ?? "null"), ("role", role), ("reason", reason), ("length", length)));

        if (target != null)
        {
            SendRoleBans(target.Value);
        }
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetServerRoleBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        if (ban.UserId is { } player && _cachedRoleBans.TryGetValue(player, out var roleBans))
        {
            roleBans.RemoveWhere(roleBan => roleBan.Id == ban.Id);
            SendRoleBans(player);
        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<string>? GetJobBans(NetUserId playerUserId)
    {
        if (!_cachedRoleBans.TryGetValue(playerUserId, out var roleBans))
            return null;
        return roleBans
            .Where(ban => ban.Role.StartsWith(JobPrefix, StringComparison.Ordinal))
            .Select(ban => ban.Role[JobPrefix.Length..])
            .ToHashSet();
    }
    #endregion

    public void SendRoleBans(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var player))
        {
            return;
        }

        SendRoleBans(player);
    }

    public void SendRoleBans(ICommonSession pSession)
    {
        var roleBans = _cachedRoleBans.GetValueOrDefault(pSession.UserId) ?? new HashSet<ServerRoleBanDef>();
        var bans = new MsgRoleBans()
        {
            Bans = roleBans.Select(o => o.Role).ToList()
        };

        _sawmill.Debug($"Sent rolebans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.ConnectedClient);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}

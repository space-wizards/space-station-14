using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace Content.Server.Administration;

public sealed class RoleBanSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<NetUserId, HashSet<string>> _cachedRoleBans = new();

    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cachedRoleBans.Clear();
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected
            || _cachedRoleBans.ContainsKey(e.Session.UserId))
            return;

        var netChannel = e.Session.ConnectedClient;
        await CacheDbRoleBans(e.Session.UserId, netChannel.RemoteEndPoint.Address, netChannel.UserData.HWId);
    }

    public async Task<bool> AddRoleBan(ServerRoleBanDef banDef, LocatedPlayerData? playerData = null)
    {
        if (banDef.UserId != null)
        {
            if (!_cachedRoleBans.TryGetValue(banDef.UserId.Value, out var roleBans))
            {
                roleBans = new HashSet<string>();
                _cachedRoleBans.Add(banDef.UserId.Value, roleBans);
            }
            if (!roleBans.Contains(banDef.Role))
                roleBans.Add(banDef.Role);
        }

        await _db.AddServerRoleBanAsync(banDef);
        return true;
    }

    public HashSet<string>? GetRoleBans(NetUserId playerUserId)
    {
        return _cachedRoleBans.TryGetValue(playerUserId, out var roleBans) ? roleBans : null;
    }

    private async Task CacheDbRoleBans(NetUserId userId, IPAddress? address = null, ImmutableArray<byte>? hwId = null)
    {
        var roleBans = await _db.GetServerRoleBansAsync(address, userId, hwId);
        if (roleBans.Count == 0) return;

        var userRoleBans = new HashSet<string>();
        foreach (var ban in roleBans)
        {
            if (userRoleBans.Contains(ban.Role))
                continue;
            userRoleBans.Add(ban.Role);
        }

        if (_cachedRoleBans.ContainsKey(userId))
            _cachedRoleBans[userId] = userRoleBans;
        else
            _cachedRoleBans.Add(userId, userRoleBans);
    }
}

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server._NullLink.Core;
using Content.Shared._NullLink;
using Content.Shared.Starlight;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.PlayerData;

public sealed partial class NullLinkPlayerManager : IPostInjectInit, INullLinkPlayerManager
{
    [Dependency] private readonly IActorRouter _actors = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly ConcurrentDictionary<Guid, PlayerData> _playerById = [];
    private ISawmill _sawmill = default!;

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("NullLink player data");
        _netMgr.RegisterNetMessage<MsgUpdatePlayerRoles>();
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        InitializeLinking();
    }

    public void Shutdown()
    {
        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        _playerById.Clear();
    }

    public ValueTask SyncRoles(PlayerRolesSyncEvent ev)
    {
        if (!_playerById.TryGetValue(ev.Player, out var playerData))
            return ValueTask.CompletedTask;
        playerData.Roles.Clear();
        playerData.Roles.UnionWith(ev.Roles);

        SendPlayerRoles(playerData.Session, playerData.Roles);
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateRoles(RolesChangedEvent ev)
    {
        if (!_playerById.TryGetValue(ev.Player, out var playerData))
            return ValueTask.CompletedTask;
        playerData.Roles.ExceptWith(ev.Remove);
        playerData.Roles.UnionWith(ev.Add);

        SendPlayerRoles(playerData.Session, playerData.Roles);
        return ValueTask.CompletedTask;
    }

    public bool TryGetPlayerData(Guid userId, [NotNullWhen(true)] out PlayerData? playerData)
        => _playerById.TryGetValue(userId, out playerData);

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Zombie:
            case SessionStatus.Connecting:
                break;
            case SessionStatus.Connected:
                if (_actors.TryGetServerGrain(out var serverGrain))
                {
                    _ = serverGrain.PlayerConnected(e.Session.UserId);
                    var state = new PlayerData
                    {
                        Session = e.Session,
                    };
                    if (!_playerById.TryAdd(e.Session.UserId, state))
                        _sawmill.Error($"Failed to add player with UserId {e.Session.UserId} to playerById dictionary.");
                }
                break;
            case SessionStatus.InGame:
                break;
            case SessionStatus.Disconnected:
                if (_actors.TryGetServerGrain(out var serverGrain2))
                    _ = serverGrain2.PlayerDisconnected(e.Session.UserId);
                _playerById.Remove(e.Session.UserId, out _);
                break;
            default:
                break;
        }
    }
    private void SendPlayerRoles(ICommonSession session, HashSet<ulong> roles)
    {
        _netMgr.ServerSendMessage(new MsgUpdatePlayerRoles
        {
            Roles = roles,
            DiscordLink = GetDiscordAuthUrl(session.UserId.ToString())
        }, session.Channel);
    }
}

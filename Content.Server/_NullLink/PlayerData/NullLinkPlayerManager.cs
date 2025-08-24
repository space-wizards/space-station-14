using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server._NullLink.Core;
using Content.Server._NullLink.Helpers;
using Content.Server.Database;
using Content.Shared._NullLink;
using Content.Shared.NullLink.CCVar;
using Content.Shared.Starlight;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Starlight.NullLink;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.PlayerData;

public sealed partial class NullLinkPlayerManager : INullLinkPlayerManager
{
    [Dependency] private readonly IActorRouter _actors = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ConcurrentDictionary<Guid, PlayerData> _playerById = [];
    private readonly ConcurrentDictionary<Guid, ICommonSession> _mentors = [];
    private ISawmill _sawmill = default!;
    private RoleRequirementPrototype? _mentorReq;
    private TitleBuilderPrototype? _builder;

    public IEnumerable<ICommonSession> Mentors => _mentors.Values;
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("NullLink player data");
        _netMgr.RegisterNetMessage<MsgUpdatePlayerRoles>();
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        InitializeLinking();
        _cfg.OnValueChanged(NullLinkCCVars.RoleReqMentors, UpdateMentors, true);
        _cfg.OnValueChanged(NullLinkCCVars.TitleBuild, UpdateTitleBuilder, true);
        _actors.OnConnected += OnNullLinkConnected;
    }

    private void OnNullLinkConnected()
    {
        if (!_actors.TryGetServerGrain(out var serverGrain))
            return;

        foreach (var player in _playerById)
            _ = serverGrain.PlayerConnected(player.Key);
    }

    public void Shutdown()
    {
        _actors.OnConnected -= OnNullLinkConnected;
        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        _playerById.Clear();
    }

    public ValueTask SyncRoles(PlayerRolesSyncEvent ev)
    {
        if (!_playerById.TryGetValue(ev.Player, out var playerData))
            return ValueTask.CompletedTask;
        playerData.Roles.Clear();
        playerData.Roles.UnionWith(ev.Roles);
        playerData.DiscordId = ev.DiscordId;

        MentorCheck(ev.Player, playerData);

        RebuildTitle(ev.Player, playerData);

        SendPlayerRoles(playerData.Session, playerData.Roles);
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateRoles(RolesChangedEvent ev)
    {
        if (!_playerById.TryGetValue(ev.Player, out var playerData))
            return ValueTask.CompletedTask;
        playerData.Roles.ExceptWith(ev.Remove);
        playerData.Roles.UnionWith(ev.Add);
        playerData.DiscordId = ev.DiscordId;

        MentorCheck(ev.Player, playerData);

        RebuildTitle(ev.Player, playerData);

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
                var state = new PlayerData
                {
                    Session = e.Session,
                };
                if (!_playerById.TryAdd(e.Session.UserId, state))
                    _sawmill.Error($"Failed to add player with UserId {e.Session.UserId} to playerById dictionary.");
                if (_actors.TryGetServerGrain(out var serverGrain))
                    serverGrain.PlayerConnected(e.Session.UserId)
                        .FireAndForget(err=> _sawmill.Error($"PlayerConnected dispatch failed: {err}"));
                SendPlayerRoles(e.Session, state.Roles);
                break;
            case SessionStatus.InGame:
                break;
            case SessionStatus.Disconnected:
                if (_actors.TryGetServerGrain(out var serverGrain2))
                    serverGrain2.PlayerDisconnected(e.Session.UserId)
                        .FireAndForget(err => _sawmill.Error($"PlayerDisconnected dispatch failed: {err}"));
                _playerById.Remove(e.Session.UserId, out _);
                _mentors.Remove(e.Session.UserId, out _);
                break;
            default:
                break;
        }
    }
    private void SendPlayerRoles(ICommonSession session, HashSet<ulong> roles)
        => _netMgr.ServerSendMessage(new MsgUpdatePlayerRoles
        {
            Roles = roles,
            DiscordLink = GetDiscordAuthUrl(session.UserId.ToString())
        }, session.Channel);

    private void UpdateMentors(string obj)
    {
        if(_mentorReq?.ID == obj)
            return; 

        _mentors.Clear();
        if (!_proto.TryIndex<RoleRequirementPrototype>(obj, out var mentorReq))
            return;
        _mentorReq = mentorReq;

        Pipe.RunInBackground(async () =>
        {
            foreach (var player in _playerById)
            {
                if (_mentorReq?.Roles.Any(player.Value.Roles.Contains) != true)
                    continue;
                _mentors.TryAdd(player.Key, player.Value.Session);
            }
        });
    }
    private void UpdateTitleBuilder(string obj)
    {
        if (_builder?.ID == obj)
            return;
        if (!_proto.TryIndex<TitleBuilderPrototype>(obj, out var builder))
            return;
        _builder = builder;

        foreach (var player in _playerById)
            RebuildTitle(player.Key, player.Value);
    }
    private void RebuildTitle(Guid player, PlayerData playerData)
    {
        if (_builder == null)
            return;

        var result = new List<string>(_builder.Segments.Count);
        foreach (var segment in _builder.Segments)
        {
            foreach (var title in segment.Titles)
            {
                if (!title.Roles.Any(playerData.Roles.Contains))
                    continue;
                if (title.Color != null)
                    result.Add($"[color={title.Color.Value.ToHex()}]{title.Text}[/color]");
                else
                    result.Add(title.Text);
                break;
            }
        }

        playerData.Title = result.Count > 0 ? string.Join(_builder.Separator, result) : null;
    }

    private void MentorCheck(Guid player, PlayerData playerData)
    {
        if (_mentorReq?.Roles.Any(playerData.Roles.Contains) == true)
            _mentors.TryAdd(player, playerData.Session);
        else
            _mentors.Remove(player, out _);
    }
}

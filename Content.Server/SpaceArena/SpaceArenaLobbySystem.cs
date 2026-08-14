using Content.Server.Chat.Managers;
using Content.Server.SpaceArena.Components;
using Content.Shared.Chat;
using Content.Shared.Maps;
using Content.Shared.SpaceArena;
using Content.Shared.SpaceArena.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SpaceArena;

public sealed partial class SpaceArenaLobbySystem : EntitySystem
{
    private const string LobbyPlayerJoinedSound = "/Audio/Effects/newplayerping.ogg";

    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SpaceArenaMatchSystem _matches = default!;
    [Dependency] private IPlayerManager _players = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceArenaPlayerLobbyComponent, SpaceArenaMatchPlayerJoinedEvent>(OnPlayerJoined);
        SubscribeLocalEvent<SpaceArenaPlayerLobbyComponent, SpaceArenaMatchPlayerLeftEvent>(OnPlayerLeft);
        SubscribeLocalEvent<SpaceArenaPlayerLobbyComponent, SpaceArenaMatchStateChangedEvent>(OnStateChanged);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public bool TryCreateLobby(
        EntProtoId mode,
        ProtoId<GameMapPrototype> arena,
        ICommonSession player,
        out EntityUid lobby)
    {
        lobby = EntityUid.Invalid;
        if (_matches.TryGetPlayerMatch(player.UserId, out _) ||
            _matches.TryGetSpectatedMatch(player.UserId, out _) ||
            !_matches.TryCreateMatch(mode, arena, out var match))
        {
            return false;
        }

        var component = EnsureComp<SpaceArenaPlayerLobbyComponent>(match);
        component.Host = player.UserId;
        component.HostName = player.Name;
        component.Mode = mode;

        if (!_matches.TryJoinMatch(match, player))
        {
            QueueDel(match);
            return false;
        }

        lobby = match;
        return true;
    }

    public bool TryJoinLobby(EntityUid lobby, ICommonSession player)
    {
        return TryComp(lobby, out SpaceArenaPlayerLobbyComponent? _) &&
               TryComp(lobby, out SpaceArenaMatchComponent? match) &&
               match.State == SpaceArenaMatchState.Waiting &&
               _matches.TryJoinMatch(lobby, player);
    }

    public bool TryStartLobby(EntityUid lobby, ICommonSession player)
    {
        return TryComp(lobby, out SpaceArenaPlayerLobbyComponent? component) &&
               component.Host == player.UserId &&
               _matches.ContainsPlayer(lobby, player.UserId) &&
               _matches.TryStartMatch(lobby);
    }

    public bool TryLeaveLobby(ICommonSession player)
    {
        if (!_matches.TryGetPlayerMatch(player.UserId, out var lobby) ||
            !TryComp(lobby, out SpaceArenaPlayerLobbyComponent? _) ||
            !TryComp(lobby, out SpaceArenaMatchComponent? match) ||
            match.State != SpaceArenaMatchState.Waiting)
        {
            return false;
        }

        return _matches.TryLeaveMatch(player);
    }

    private void OnPlayerJoined(
        Entity<SpaceArenaPlayerLobbyComponent> lobby,
        ref SpaceArenaMatchPlayerJoinedEvent args)
    {
        if (TryComp(lobby, out SpaceArenaMatchComponent? match) &&
            TryComp(lobby, out SpaceArenaMatchRuntimeComponent? runtime))
        {
            var name = _players.TryGetSessionById(args.Player, out var joined)
                ? joined.Name
                : Loc.GetString("space-arena-lobby-unknown-host");
            var message = Loc.GetString(
                "space-arena-lobby-player-joined",
                ("player", name),
                ("players", runtime.Players.Count),
                ("max", match.MaxPlayers));
            var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

            foreach (var player in runtime.Players.Keys)
            {
                if (player == args.Player || !_players.TryGetSessionById(player, out var session))
                    continue;

                _chat.ChatMessageToOne(
                    ChatChannel.Server,
                    message,
                    wrapped,
                    EntityUid.Invalid,
                    false,
                    session.Channel,
                    audioPath: LobbyPlayerJoinedSound,
                    audioVolume: -10f);
            }
        }

        RaiseLobbyChanged(lobby.Owner);
    }

    private void OnPlayerLeft(
        Entity<SpaceArenaPlayerLobbyComponent> lobby,
        ref SpaceArenaMatchPlayerLeftEvent args)
    {
        if (!TryComp(lobby, out SpaceArenaMatchComponent? match))
            return;

        if (lobby.Comp.Host == args.Player)
        {
            if (!_matches.TryGetFirstPlayer(lobby, out var nextHost))
            {
                if (match.State == SpaceArenaMatchState.Waiting)
                    QueueDel(lobby);

                RaiseLobbyChanged(lobby.Owner);
                return;
            }

            lobby.Comp.Host = nextHost;
            lobby.Comp.HostName = _players.TryGetSessionById(nextHost, out var session)
                ? session.Name
                : Loc.GetString("space-arena-lobby-unknown-host");
        }

        RaiseLobbyChanged(lobby.Owner);
    }

    private void OnStateChanged(
        Entity<SpaceArenaPlayerLobbyComponent> lobby,
        ref SpaceArenaMatchStateChangedEvent args)
    {
        RaiseLobbyChanged(lobby.Owner);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected ||
            !_matches.TryGetPlayerMatch(args.Session.UserId, out var lobby) ||
            !TryComp(lobby, out SpaceArenaPlayerLobbyComponent? _) ||
            !TryComp(lobby, out SpaceArenaMatchComponent? match) ||
            match.State != SpaceArenaMatchState.Waiting)
        {
            return;
        }

        _matches.TryLeaveMatch(args.Session);
    }

    private void RaiseLobbyChanged(EntityUid lobby)
    {
        var ev = new SpaceArenaPlayerLobbyChangedEvent();
        RaiseLocalEvent(lobby, ref ev);
    }
}

[ByRefEvent]
public readonly record struct SpaceArenaPlayerLobbyChangedEvent;

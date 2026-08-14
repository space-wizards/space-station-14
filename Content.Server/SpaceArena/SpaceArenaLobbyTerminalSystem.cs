using Content.Server.SpaceArena.Components;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Maps;
using Content.Shared.SpaceArena;
using Content.Shared.SpaceArena.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SpaceArena;

public sealed partial class SpaceArenaLobbyTerminalSystem : EntitySystem
{
    [Dependency] private SpaceArenaLobbySystem _lobbies = default!;
    [Dependency] private SpaceArenaMatchSystem _matches = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    private readonly HashSet<EntityUid> _excludedLobbies = [];
    private readonly Dictionary<NetUserId, SpaceArenaLobbyEui> _openEuis = [];

    private bool _terminalsDirty;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceArenaPlayerLobbyComponent, SpaceArenaPlayerLobbyChangedEvent>(OnLobbyChanged);
        SubscribeLocalEvent<SpaceArenaPlayerLobbyComponent, ComponentShutdown>(OnLobbyShutdown);
        SubscribeNetworkEvent<SpaceArenaOpenLobbyRequest>(OnOpenLobbyRequest);
        Subs.BuiEvents<SpaceArenaLobbyTerminalComponent>(SpaceArenaLobbyUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<SpaceArenaCreateLobbyMessage>(OnCreateLobby);
            subs.Event<SpaceArenaJoinLobbyMessage>(OnJoinLobby);
            subs.Event<SpaceArenaStartLobbyMessage>(OnStartLobby);
            subs.Event<SpaceArenaSpectateLobbyMessage>(OnSpectateLobby);
            subs.Event<SpaceArenaLeaveLobbyMessage>(OnLeaveLobby);
            subs.Event<SpaceArenaLobbyStatusRequestMessage>(OnStatusRequest);
        });
    }

    private void OnOpenLobbyRequest(SpaceArenaOpenLobbyRequest args, EntitySessionEventArgs sessionArgs)
    {
        if (sessionArgs.SenderSession.AttachedEntity is not { } actor ||
            TerminatingOrDeleted(actor))
        {
            return;
        }

        var query = EntityQueryEnumerator<SpaceArenaLobbyTerminalComponent>();
        while (query.MoveNext(out var terminal, out _))
        {
            if (TerminatingOrDeleted(terminal) || EntityManager.IsQueuedForDeletion(terminal))
                continue;

            if (_openEuis.TryGetValue(sessionArgs.SenderSession.UserId, out var existing))
                existing.Close();

            var eui = new SpaceArenaLobbyEui(terminal, this);
            _openEuis[sessionArgs.SenderSession.UserId] = eui;
            _eui.OpenEui(eui, sessionArgs.SenderSession);
            return;
        }
    }

    public void OnEuiClosed(SpaceArenaLobbyEui eui)
    {
        if (_openEuis.TryGetValue(eui.Player.UserId, out var openEui) && openEui == eui)
            _openEuis.Remove(eui.Player.UserId);
    }

    public SpaceArenaLobbyEuiState GetEuiState(EntityUid terminalUid, ICommonSession player)
    {
        if (!TryComp(terminalUid, out SpaceArenaLobbyTerminalComponent? terminal))
            return new SpaceArenaLobbyEuiState([], [], [], null, null, false);

        var modes = BuildModeOptions(terminal);
        var arenas = BuildArenaOptions(terminal);
        var rooms = BuildRooms(terminal, null);
        GetUserStatus(player.UserId, out var currentLobby, out var spectatedMatch, out var canManageLobbies);
        return new SpaceArenaLobbyEuiState(
            modes,
            arenas,
            rooms,
            currentLobby,
            spectatedMatch,
            canManageLobbies);
    }

    public void HandleEuiMessage(SpaceArenaLobbyEui eui, EuiMessageBase message)
    {
        if (!TryComp(eui.Terminal, out SpaceArenaLobbyTerminalComponent? terminal))
        {
            eui.Close();
            return;
        }

        switch (message)
        {
            case SpaceArenaCreateLobbyEuiMessage create
                when terminal.Modes.Contains(create.Mode) && terminal.Arenas.Contains(create.Arena):
                _lobbies.TryCreateLobby(create.Mode, create.Arena, eui.Player, out _);
                break;
            case SpaceArenaJoinLobbyEuiMessage join
                when TryGetEntity(join.Lobby, out var lobby) &&
                     lobby is { } lobbyUid &&
                     IsAllowedLobby(terminal, lobbyUid):
                _lobbies.TryJoinLobby(lobbyUid, eui.Player);
                break;
            case SpaceArenaStartLobbyEuiMessage start
                when TryGetEntity(start.Lobby, out var lobby) &&
                     lobby is { } lobbyUid &&
                     IsAllowedLobby(terminal, lobbyUid):
                if (_lobbies.TryStartLobby(lobbyUid, eui.Player))
                    eui.Close();
                break;
            case SpaceArenaSpectateLobbyEuiMessage spectate
                when TryGetEntity(spectate.Lobby, out var lobby) &&
                     lobby is { } lobbyUid &&
                     IsAllowedSpectatableMatch(terminal, lobbyUid):
                if (_matches.TrySpectateMatch(lobbyUid, eui.Player))
                    eui.Close();
                break;
            case SpaceArenaLeaveLobbyEuiMessage:
                _lobbies.TryLeaveLobby(eui.Player);
                break;
        }
    }

    private void OnUiOpened(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref BoundUIOpenedEvent args)
    {
        UpdateUi(terminal);
        SendUserStatus(terminal, args.Actor);
    }

    private void OnCreateLobby(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref SpaceArenaCreateLobbyMessage args)
    {
        if (!TryGetSession(args.Actor, out var session) ||
            !terminal.Comp.Modes.Contains(args.Mode) ||
            !terminal.Comp.Arenas.Contains(args.Arena))
        {
            return;
        }

        _lobbies.TryCreateLobby(args.Mode, args.Arena, session, out _);
        SendUserStatus(terminal, args.Actor);
    }

    private void OnJoinLobby(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref SpaceArenaJoinLobbyMessage args)
    {
        if (!TryGetSession(args.Actor, out var session) ||
            !TryGetEntity(args.Lobby, out var lobby) ||
            lobby is not { } lobbyUid ||
            !IsAllowedLobby(terminal.Comp, lobbyUid))
        {
            return;
        }

        _lobbies.TryJoinLobby(lobbyUid, session);
        SendUserStatus(terminal, args.Actor);
    }

    private void OnStartLobby(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref SpaceArenaStartLobbyMessage args)
    {
        if (!TryGetSession(args.Actor, out var session) ||
            !TryGetEntity(args.Lobby, out var lobby) ||
            lobby is not { } lobbyUid ||
            !IsAllowedLobby(terminal.Comp, lobbyUid))
        {
            return;
        }

        _lobbies.TryStartLobby(lobbyUid, session);
        SendUserStatus(terminal, args.Actor);
    }

    private void OnSpectateLobby(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref SpaceArenaSpectateLobbyMessage args)
    {
        if (!TryGetSession(args.Actor, out var session) ||
            !TryGetEntity(args.Lobby, out var lobby) ||
            lobby is not { } lobbyUid ||
            !IsAllowedSpectatableMatch(terminal.Comp, lobbyUid))
        {
            return;
        }

        _matches.TrySpectateMatch(lobbyUid, session);
    }

    private void OnLeaveLobby(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref SpaceArenaLeaveLobbyMessage args)
    {
        if (!TryGetSession(args.Actor, out var session))
            return;

        _lobbies.TryLeaveLobby(session);
        SendUserStatus(terminal, args.Actor);
    }

    private void OnStatusRequest(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        ref SpaceArenaLobbyStatusRequestMessage args)
    {
        SendUserStatus(terminal, args.Actor);
    }

    private void OnLobbyChanged(
        Entity<SpaceArenaPlayerLobbyComponent> lobby,
        ref SpaceArenaPlayerLobbyChangedEvent args)
    {
        QueueTerminalRefresh();
    }

    private void OnLobbyShutdown(
        Entity<SpaceArenaPlayerLobbyComponent> lobby,
        ref ComponentShutdown args)
    {
        QueueTerminalRefresh(lobby.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_terminalsDirty)
            return;

        _terminalsDirty = false;
        RefreshTerminals(_excludedLobbies);
        _excludedLobbies.Clear();
    }

    private void QueueTerminalRefresh(EntityUid? excludedLobby = null)
    {
        _terminalsDirty = true;
        if (excludedLobby is { } lobby)
            _excludedLobbies.Add(lobby);
    }

    private bool TryGetSession(EntityUid actor, out ICommonSession session)
    {
        session = default!;
        if (!TryComp(actor, out ActorComponent? actorComponent))
            return false;

        session = actorComponent.PlayerSession;
        return true;
    }

    private bool IsAllowedLobby(SpaceArenaLobbyTerminalComponent terminal, EntityUid lobby)
    {
        return !TerminatingOrDeleted(lobby) &&
               !EntityManager.IsQueuedForDeletion(lobby) &&
               TryComp(lobby, out SpaceArenaPlayerLobbyComponent? playerLobby) &&
               TryComp(lobby, out SpaceArenaMatchComponent? match) &&
               match.State == SpaceArenaMatchState.Waiting &&
               match.Arena is { } arena &&
               terminal.Modes.Contains(playerLobby.Mode) &&
               terminal.Arenas.Contains(arena);
    }

    private bool IsAllowedSpectatableMatch(SpaceArenaLobbyTerminalComponent terminal, EntityUid lobby)
    {
        return !TerminatingOrDeleted(lobby) &&
               !EntityManager.IsQueuedForDeletion(lobby) &&
               TryComp(lobby, out SpaceArenaPlayerLobbyComponent? playerLobby) &&
               TryComp(lobby, out SpaceArenaMatchComponent? match) &&
               (match.State is SpaceArenaMatchState.Preparing or
                   SpaceArenaMatchState.Countdown or
                   SpaceArenaMatchState.Active) &&
               match.Arena is { } arena &&
               terminal.Modes.Contains(playerLobby.Mode) &&
               terminal.Arenas.Contains(arena);
    }

    private void RefreshTerminals(HashSet<EntityUid> excludedLobbies)
    {
        var query = EntityQueryEnumerator<SpaceArenaLobbyTerminalComponent>();
        while (query.MoveNext(out var uid, out var terminal))
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            var entity = (uid, terminal);
            UpdateUi(entity, excludedLobbies);
            SendOpenUserStatuses(entity);
        }

        foreach (var eui in _openEuis.Values)
            eui.StateDirty();
    }

    private void SendOpenUserStatuses(Entity<SpaceArenaLobbyTerminalComponent> terminal)
    {
        foreach (var actor in _ui.GetActors(terminal.Owner, SpaceArenaLobbyUiKey.Key))
        {
            if (!TerminatingOrDeleted(actor))
                SendUserStatus(terminal, actor);
        }
    }

    private void UpdateUi(
        Entity<SpaceArenaLobbyTerminalComponent> terminal,
        HashSet<EntityUid>? excludedLobbies = null)
    {
        var modes = BuildModeOptions(terminal.Comp);
        var arenas = BuildArenaOptions(terminal.Comp);
        var rooms = BuildRooms(terminal.Comp, excludedLobbies);
        var state = new SpaceArenaLobbyBoundUserInterfaceState(modes, arenas, rooms);
        _ui.SetUiState(terminal.Owner, SpaceArenaLobbyUiKey.Key, state);
    }

    private List<SpaceArenaLobbyModeOption> BuildModeOptions(SpaceArenaLobbyTerminalComponent terminal)
    {
        var options = new List<SpaceArenaLobbyModeOption>(terminal.Modes.Count);
        foreach (var mode in terminal.Modes)
        {
            if (!ProtoMan.TryIndex(mode, out EntityPrototype? prototype) ||
                !prototype.TryComp(out SpaceArenaMatchComponent? match, Factory))
            {
                continue;
            }

            options.Add(new SpaceArenaLobbyModeOption(mode, match.Name));
        }

        return options;
    }

    private List<SpaceArenaLobbyArenaOption> BuildArenaOptions(SpaceArenaLobbyTerminalComponent terminal)
    {
        var options = new List<SpaceArenaLobbyArenaOption>(terminal.Arenas.Count);
        foreach (var arenaId in terminal.Arenas)
        {
            if (!ProtoMan.TryIndex(arenaId, out var arena) || arena.SpaceArena is not { } arenaData)
                continue;

            var supportedModes = new List<EntProtoId>(arenaData.Modes.Count);
            foreach (var mode in arenaData.Modes)
            {
                if (terminal.Modes.Contains(mode))
                    supportedModes.Add(mode);
            }

            if (supportedModes.Count == 0)
                continue;

            options.Add(new SpaceArenaLobbyArenaOption(
                arenaId,
                arena.MapName,
                arenaData.LobbyFormat,
                arenaData.PreviewWeapon,
                (int) Math.Min(arena.MinPlayers, int.MaxValue),
                (int) Math.Min(arena.MaxPlayers, int.MaxValue),
                supportedModes));
        }

        return options;
    }

    private List<SpaceArenaLobbyRoom> BuildRooms(
        SpaceArenaLobbyTerminalComponent terminal,
        HashSet<EntityUid>? excludedLobbies)
    {
        var rooms = new List<SpaceArenaLobbyRoom>();
        var query = EntityQueryEnumerator<SpaceArenaPlayerLobbyComponent, SpaceArenaMatchComponent>();
        while (query.MoveNext(out var uid, out var lobby, out var match))
        {
            if (excludedLobbies?.Contains(uid) == true ||
                TerminatingOrDeleted(uid) ||
                EntityManager.IsQueuedForDeletion(uid) ||
                (match.State is SpaceArenaMatchState.Ending or
                    SpaceArenaMatchState.Finished or
                    SpaceArenaMatchState.Cleanup) ||
                match.PlayerCount <= 0 ||
                match.Arena is not { } arenaId ||
                !terminal.Modes.Contains(lobby.Mode) ||
                !terminal.Arenas.Contains(arenaId) ||
                !ProtoMan.TryIndex(arenaId, out var arena))
            {
                continue;
            }

            rooms.Add(new SpaceArenaLobbyRoom(
                GetNetEntity(uid),
                lobby.Host,
                lobby.HostName,
                match.Name,
                arena.MapName,
                match.PlayerCount,
                match.MinPlayers,
                match.MaxPlayers,
                match.State));
        }

        return rooms;
    }

    private void SendUserStatus(Entity<SpaceArenaLobbyTerminalComponent> terminal, EntityUid actor)
    {
        if (!TryComp(actor, out ActorComponent? actorComponent))
            return;

        GetUserStatus(
            actorComponent.PlayerSession.UserId,
            out var currentLobby,
            out var spectatedMatch,
            out var canManageLobbies);

        _ui.ServerSendUiMessage(
            terminal.Owner,
            SpaceArenaLobbyUiKey.Key,
            new SpaceArenaLobbyUserStatusMessage(currentLobby, spectatedMatch, canManageLobbies),
            actor);
    }

    private void GetUserStatus(
        NetUserId player,
        out NetEntity? currentLobby,
        out NetEntity? spectatedMatch,
        out bool canManageLobbies)
    {
        currentLobby = null;
        spectatedMatch = null;
        canManageLobbies = true;
        if (_matches.TryGetPlayerMatch(player, out var lobby))
        {
            canManageLobbies = false;
            if (TryComp(lobby, out SpaceArenaPlayerLobbyComponent? _) &&
                TryComp(lobby, out SpaceArenaMatchComponent? match) &&
                match.State == SpaceArenaMatchState.Waiting)
            {
                currentLobby = GetNetEntity(lobby);
                canManageLobbies = true;
            }
        }
        else if (_matches.TryGetSpectatedMatch(player, out var spectated))
        {
            spectatedMatch = GetNetEntity(spectated);
            canManageLobbies = false;
        }
    }
}

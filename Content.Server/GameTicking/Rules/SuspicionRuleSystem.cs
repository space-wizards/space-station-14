using System.Linq;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Station.Components;
using Content.Server.Suspicion;
using Content.Server.Suspicion.Roles;
using Content.Server.Traitor.Uplink;
using Content.Shared.CCVar;
using Content.Shared.Doors.Systems;
using Content.Shared.EntityList;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Suspicion;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
///     Simple GameRule that will do a TTT-like gamemode with traitors.
/// </summary>
public sealed class SuspicionRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    public override string Prototype => "Suspicion";

    private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(1);

    private readonly HashSet<SuspicionRoleComponent> _traitors = new();

    public IReadOnlyCollection<SuspicionRoleComponent> Traitors => _traitors;

    [DataField("addedSound")] private SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");

    private CancellationTokenSource _checkTimerCancel = new();
    private TimeSpan? _endTime;

    public TimeSpan? EndTime
    {
        get => _endTime;
        set
        {
            _endTime = value;
            SendUpdateToAll();
        }
    }

    public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromSeconds(CCVars.SuspicionMaxTimeSeconds.DefaultValue);
    public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

    private const string TraitorID = "SuspicionTraitor";
    private const string InnocentID = "SuspicionInnocent";
    private const string SuspicionLootTable = "SuspicionRule";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersAssigned);
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStartAttempt);
        SubscribeLocalEvent<RefreshLateJoinAllowedEvent>(OnLateJoinRefresh);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

        SubscribeLocalEvent<SuspicionRoleComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SuspicionRoleComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SuspicionRoleComponent, RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<SuspicionRoleComponent, RoleRemovedEvent>(OnRoleRemoved);
    }

    private void OnRoundStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.SuspicionMinPlayers);

        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement($"Not enough players readied up for the game! There were {ev.Players.Length} players readied up out of {minPlayers} needed.");
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement("No players readied up! Can't start Suspicion.");
            ev.Cancel();
        }
    }

    private void OnPlayersAssigned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        var minTraitors = _cfg.GetCVar(CCVars.SuspicionMinTraitors);
        var playersPerTraitor = _cfg.GetCVar(CCVars.SuspicionPlayersPerTraitor);
        var traitorStartingBalance = _cfg.GetCVar(CCVars.SuspicionStartingBalance);

        var list = new List<IPlayerSession>(ev.Players);
        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId) || player.AttachedEntity is not {} attached)
            {
                continue;
            }
            prefList.Add(player);

            attached.EnsureComponent<SuspicionRoleComponent>();
        }

        // Max is players-1 so there's always at least one innocent.
        var numTraitors = MathHelper.Clamp(ev.Players.Length / playersPerTraitor,
            minTraitors, ev.Players.Length-1);

        var traitors = new List<SuspicionTraitorRole>();

        for (var i = 0; i < numTraitors; i++)
        {
            IPlayerSession traitor;
            if(prefList.Count == 0)
            {
                if (list.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
                    break;
                }
                traitor = _random.PickAndTake(list);
                Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
            }
            else
            {
                traitor = _random.PickAndTake(prefList);
                list.Remove(traitor);
                Logger.InfoS("preset", "Selected a preferred traitor.");
            }
            var mind = traitor.Data.ContentData()?.Mind;
            var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorID);

            DebugTools.AssertNotNull(mind?.OwnedEntity);

            var traitorRole = new SuspicionTraitorRole(mind!, antagPrototype);
            mind!.AddRole(traitorRole);
            traitors.Add(traitorRole);

            // try to place uplink
            _uplink.AddUplink(mind.OwnedEntity!.Value, traitorStartingBalance);
        }

        foreach (var player in list)
        {
            var mind = player.Data.ContentData()?.Mind;
            var antagPrototype = _prototypeManager.Index<AntagPrototype>(InnocentID);

            DebugTools.AssertNotNull(mind);

            mind!.AddRole(new SuspicionInnocentRole(mind, antagPrototype));
        }

        foreach (var traitor in traitors)
        {
            traitor.GreetSuspicion(traitors, _chatManager);
        }
    }

    public override void Started()
    {
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;

        RoundMaxTime = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.SuspicionMaxTimeSeconds));

        EndTime = _timing.CurTime + RoundMaxTime;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-added-announcement"));

        var filter = Filter.Empty()
            .AddWhere(session => ((IPlayerSession) session).ContentData()?.Mind?.HasRole<SuspicionTraitorRole>() ?? false);

        SoundSystem.Play(_addedSound.GetSound(), filter, AudioParams.Default);

        _doorSystem.AccessType = SharedDoorSystem.AccessTypes.AllowAllNoExternal;

        var susLoot = _prototypeManager.Index<EntityLootTablePrototype>(SuspicionLootTable);

        foreach (var (_, mapGrid) in EntityManager.EntityQuery<StationMemberComponent, MapGridComponent>(true))
        {
            // I'm so sorry.
            var tiles = mapGrid.GetAllTiles().ToArray();
            Logger.Info($"TILES: {tiles.Length}");

            var spawn = susLoot.GetSpawns();
            var count = spawn.Count;

            // Try to scale spawned amount by station size...
            if (tiles.Length < 1000)
            {
                count = Math.Min(count, tiles.Length / 10);

                // Shuffle so we pick items at random.
                _random.Shuffle(spawn);
            }

            for (var i = 0; i < count; i++)
            {
                var item = spawn[i];

                // Maximum number of attempts for trying to find a suitable empty tile.
                // We do this because we don't want to hang the server when a devious map has literally no free tiles.
                const int maxTries = 100;

                for (var j = 0; j < maxTries; j++)
                {
                    var tile = _random.Pick(tiles);

                    // Let's not spawn things on top of walls.
                    if (tile.IsBlockedTurf(false, _lookupSystem) || tile.IsSpace(_tileDefMan))
                        continue;

                    var uid = Spawn(item, tile.GridPosition(_mapManager));

                    // Keep track of all suspicion-spawned weapons so we can clean them up once the rule ends.
                    EnsureComp<SuspicionItemComponent>(uid);
                    break;
                }
            }
        }

        _checkTimerCancel = new CancellationTokenSource();
        Timer.SpawnRepeating(DeadCheckDelay, CheckWinConditions, _checkTimerCancel.Token);
    }

    public override void Ended()
    {
        _doorSystem.AccessType = SharedDoorSystem.AccessTypes.Id;
        EndTime = null;
        _traitors.Clear();

        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;

        // Clean up all items we spawned before...
        foreach (var item in EntityManager.EntityQuery<SuspicionItemComponent>(true))
        {
            Del(item.Owner);
        }

        _checkTimerCancel.Cancel();
    }

    private void CheckWinConditions()
    {
        if (!RuleAdded || !_cfg.GetCVar(CCVars.GameLobbyEnableWin))
            return;

        var traitorsAlive = 0;
        var innocentsAlive = 0;

        foreach (var playerSession in _playerManager.ServerSessions)
        {
            if (playerSession.AttachedEntity is not {Valid: true} playerEntity
                || !TryComp(playerEntity, out MobStateComponent? mobState)
                || !HasComp<SuspicionRoleComponent>(playerEntity))
            {
                continue;
            }

            if (!_mobStateSystem.IsAlive(playerEntity, mobState))
            {
                continue;
            }

            var mind = playerSession.ContentData()?.Mind;

            if (mind != null && mind.HasRole<SuspicionTraitorRole>())
                traitorsAlive++;
            else
                innocentsAlive++;
        }

        if (innocentsAlive + traitorsAlive == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-stalemate"));
            EndRound(Victory.Stalemate);
        }

        else if (traitorsAlive == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-station-win"));
            EndRound(Victory.Innocents);
        }
        else if (innocentsAlive == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-traitor-win"));
            EndRound(Victory.Traitors);
        }
        else if (_timing.CurTime > _endTime)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-traitor-time-has-run-out"));
            EndRound(Victory.Innocents);
        }
    }

    private enum Victory
    {
        Stalemate,
        Innocents,
        Traitors
    }

    private void EndRound(Victory victory)
    {
        string text;

        switch (victory)
        {
            case Victory.Innocents:
                text = Loc.GetString("rule-suspicion-end-round-innocents-victory");
                break;
            case Victory.Traitors:
                text = Loc.GetString("rule-suspicion-end-round-traitors-victory");
                break;
            default:
                text = Loc.GetString("rule-suspicion-end-round-nobody-victory");
                break;
        }

        GameTicker.EndRound(text);

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds", (int) RoundEndDelay.TotalSeconds)));
        _checkTimerCancel.Cancel();

        Timer.Spawn(RoundEndDelay, () => GameTicker.RestartRound());
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            SendUpdateTimerMessage(e.Session);
        }
    }

    private void SendUpdateToAll()
    {
        foreach (var player in _playerManager.ServerSessions.Where(p => p.Status == SessionStatus.InGame))
        {
            SendUpdateTimerMessage(player);
        }
    }

    private void SendUpdateTimerMessage(IPlayerSession player)
    {
        var msg = new SuspicionMessages.SetSuspicionEndTimerMessage
        {
            EndTime = EndTime
        };

        EntityManager.EntityNetManager?.SendSystemNetworkMessage(msg, player.ConnectedClient);
    }

    public void AddTraitor(SuspicionRoleComponent role)
    {
        if (!_traitors.Add(role))
        {
            return;
        }

        foreach (var traitor in _traitors)
        {
            traitor.AddAlly(role);
        }

        role.SetAllies(_traitors);
    }

    public void RemoveTraitor(SuspicionRoleComponent role)
    {
        if (!_traitors.Remove(role))
        {
            return;
        }

        foreach (var traitor in _traitors)
        {
            traitor.RemoveAlly(role);
        }

        role.ClearAllies();
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        EndTime = null;
        _traitors.Clear();
    }

    private void OnPlayerDetached(EntityUid uid, SuspicionRoleComponent component, PlayerDetachedEvent args)
    {
        component.SyncRoles();
    }

    private void OnPlayerAttached(EntityUid uid, SuspicionRoleComponent component, PlayerAttachedEvent args)
    {
        component.SyncRoles();
    }

    private void OnRoleAdded(EntityUid uid, SuspicionRoleComponent component, RoleAddedEvent args)
    {
        if (args.Role is not SuspicionRole role) return;
        component.Role = role;
    }

    private void OnRoleRemoved(EntityUid uid, SuspicionRoleComponent component, RoleRemovedEvent args)
    {
        if (args.Role is not SuspicionRole) return;
        component.Role = null;
    }

    private void OnLateJoinRefresh(RefreshLateJoinAllowedEvent ev)
    {
        if (!RuleAdded)
            return;

        ev.Disallow();
    }
}

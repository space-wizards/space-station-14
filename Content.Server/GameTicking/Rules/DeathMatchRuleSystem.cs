using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Points;
using Content.Shared.Storage;
using Content.Server.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Shared.Mobs.Systems;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Manages <see cref="DeathMatchRuleComponent"/>
/// </summary>
public sealed class DeathMatchRuleSystem : GameRuleSystem<DeathMatchRuleComponent>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    private ISawmill _sawmill = default!;
    int _deathMatchStartingBalance;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<DeathMatchRuleComponent, PlayerPointChangedEvent>(OnPointChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);

        _cfg.OnValueChanged(CCVars.TraitorDeathMatchStartingBalance, SetDeathMatchStartingBalance, true);
    }

    private void SetDeathMatchStartingBalance(int value) => _deathMatchStartingBalance = value;

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.TraitorDeathMatchStartingBalance, SetDeathMatchStartingBalance);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, RespawnTrackerComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var tracker, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);
            SetOutfitCommand.SetOutfit(mob, dm.Gear, EntityManager);
            EnsureComp<KillTrackerComponent>(mob);
            _respawn.AddToTracker(ev.Player.UserId, uid, tracker);

            _point.EnsurePlayer(ev.Player.UserId, uid, point);
            AddUplink(ev.Player);

            ev.Handled = true;
            break;
        }
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            _respawn.AddToTracker(ev.Mob, uid, tracker);
        }
    }

    protected override void ActiveTick(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        // This is ridiculous
        if (component.SelectionStatus == DeathMatchRuleComponent.SelectionState.ReadyToSelect)
        {
            foreach (var playerSession in _player.Sessions)
                AddUplink(playerSession);

            component.SelectionStatus = DeathMatchRuleComponent.SelectionState.SelectionMade;
        }
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            // YOU SUICIDED OR GOT THROWN INTO LAVA!
            // WHAT A GIANT FUCKING NERD! LAUGH NOW!
            if (ev.Primary is not KillPlayerSource player)
            {
                _point.AdjustPointValue(ev.Entity, -1, uid, point);
                continue;
            }

            _point.AdjustPointValue(player.PlayerId, 1, uid, point);

            if (ev.Assist is KillPlayerSource assist && dm.Victor == null)
                _point.AdjustPointValue(assist.PlayerId, 1, uid, point);

            var spawns = EntitySpawnCollection.GetSpawns(dm.RewardSpawns).Cast<string?>().ToList();
            EntityManager.SpawnEntities(Transform(ev.Entity).MapPosition, spawns);
        }
    }

    private void OnPointChanged(EntityUid uid, DeathMatchRuleComponent component, ref PlayerPointChangedEvent args)
    {
        if (component.Victor != null)
            return;

        if (args.Points < component.KillCap)
            return;

        component.Victor = args.Player;
        _roundEnd.EndRound(component.RestartDelay);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dm, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, rule))
                continue;

            if (dm.Victor != null && _player.TryGetPlayerData(dm.Victor.Value, out var data))
            {
                ev.AddLine(Loc.GetString("point-scoreboard-winner", ("player", data.UserName)));
                ev.AddLine("");
            }
            ev.AddLine(Loc.GetString("point-scoreboard-header"));
            ev.AddLine(new FormattedMessage(point.Scoreboard).ToMarkup());
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var dmPlayer, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            dmPlayer.SelectionStatus = DeathMatchRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    public void AddUplink(ICommonSession session)
    {
        if (session?.AttachedEntity is not { } user) { return; }
        if (!_uplink.AddUplink(user, _deathMatchStartingBalance)) { }
    }
}

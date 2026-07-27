// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Cargo.Prototypes;
using Content.Server.GameTicking.Rules;
using Content.Server.NukeOps;
using Content.Server.Station.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Shuttles.Components;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.NukeOps;
using Content.Server.AlertLevel;
using Content.Shared.Cargo.Components;
using Content.Server.DeadSpace.ERT;
using Content.Shared.DeadSpace.ERT.Prototypes;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Server.DeadSpace.NoShuttleFTL;
using Content.Server.GameTicking;
using Content.Server.Database;
using Content.Shared.DeadSpace.TheCircle.Shuttles;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class CircleOpsRuleSystem : GameRuleSystem<CircleOpsRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindow = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ErtResponseSystem _ertResponseSystem = default!;
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    private const int AdditionalSupport = 100000;
    private static readonly ProtoId<CargoAccountPrototype> Account = "Security";
    private static readonly ProtoId<NpcFactionPrototype> Faction = "Necromorfs";
    private static readonly ProtoId<ErtTeamPrototype> ErtTeam = "BSAA";
    private static readonly TimeSpan CountdownRoundEndTime = TimeSpan.FromSeconds(30);
    private static readonly EntProtoId ObeliskRule = "GiftNecroobeliskArtefact";
    private static string AlertLevel { get; } = "sierra";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WarDeclaredEvent>(OnWarDeclared);
        SubscribeLocalEvent<CircleOpsRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
        SubscribeLocalEvent<CircleOpsRuleComponent, StageObeliskEvent>(OnStageObelisk);
        SubscribeLocalEvent<CircleOpsRuleComponent, SpawnNecroMoonEvent>(OnSpawnNecroMoon);
    }

    protected override void AppendRoundEndText(EntityUid uid, CircleOpsRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var winText = Loc.GetString($"thecircle-{(component.State == CircleOpsState.Convergence ? "opsmajor" : "crewmajor")}");
        args.AddLine(winText);

        var winner = component.State == CircleOpsState.Convergence
            ? BiStatWinner.Antagonist
            : BiStatWinner.Crew;

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await _db.AddBiStatAsync("Юнитологи оперативники", winner, DateTime.UtcNow);
            }
            catch
            {

            }
        });
    }

    protected override void AppendRoundEndDiscordText(EntityUid uid,
        CircleOpsRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndDiscordTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("thecircle-list-start"));

        var antags = _antag.GetAntagIdentifiers(uid);
        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("thecircle-initial-name", ("name", name), ("user", sessionData.UserName)));
        }

        args.AddLine("");
    }

    protected override void AppendAdminStatus(EntityUid uid,
        CircleOpsRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var state = Loc.GetString($"game-rule-admin-status-circle-stage-{component.State.ToString().ToLowerInvariant()}");
        var activation = component.State switch
        {
            CircleOpsState.Convergence => Loc.GetString("game-rule-admin-status-circle-obelisk-convergence"),
            CircleOpsState.ObeliskActivated => Loc.GetString("game-rule-admin-status-circle-obelisk-active"),
            _ => Loc.GetString("game-rule-admin-status-circle-obelisk-inactive"),
        };

        var lines = new List<string>
        {
            Loc.GetString("game-rule-admin-status-circle-summary",
                ("stage", state),
                ("activation", activation)),
        };

        if (component.State == CircleOpsState.ObeliskActivated)
        {
            var remaining = TimeSpan.FromSeconds(_timedWindow.GetSecondsRemaining(component.WindowUntilSpawnMoon));
            lines.Add(Loc.GetString("game-rule-admin-status-circle-countdown",
                ("time", remaining.ToString(@"mm\:ss"))));
        }

        args.AddSection(Loc.GetString("game-rule-admin-status-circle-title"), lines);
    }

    protected override void Started(EntityUid uid, CircleOpsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        _timedWindow.Reset(component.WindowBefoteWarDeclaration);
        _timedWindow.Reset(component.WindowUntilSendObelisk);

        var eligible = new List<Entity<StationEventEligibleComponent, NpcFactionMemberComponent>>();
        var eligibleQuery = EntityQueryEnumerator<StationEventEligibleComponent, NpcFactionMemberComponent>();
        while (eligibleQuery.MoveNext(out var eligibleUid, out var eligibleComp, out var member))
        {
            if (!_npcFaction.IsFactionHostile(Faction, (eligibleUid, member)))
                continue;

            eligible.Add((eligibleUid, eligibleComp, member));
        }

        if (eligible.Count == 0)
            return;

        component.TargetStation = _random.Pick(eligible);
    }

    protected override void ActiveTick(EntityUid uid, CircleOpsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (_timedWindow.IsExpired(component.WindowUntilSendObelisk)
            && component.State == CircleOpsState.Inactive)
        {
            component.State = CircleOpsState.Preparing;
            GameTicker.StartGameRule(ObeliskRule);
        }

        if (component.State == CircleOpsState.WarDeclared
            && _timedWindow.IsExpired(component.WindowAfterWarDeclare)
            && component.Shuttle.HasValue
            && HasComp<NoShuttleFTLComponent>(component.Shuttle.Value))
        {
            RemComp<NoShuttleFTLComponent>(component.Shuttle.Value);
            if (TryComp<CirclePrimaryShuttleComponent>(component.Shuttle.Value, out var shuttle))
            {
                shuttle.Unlocked = true;
                Dirty(component.Shuttle.Value, shuttle);
            }
        }

        if (component.State == CircleOpsState.ObeliskActivated
            && _timedWindow.IsExpired(component.WindowUntilSpawnMoon)
            && component.Obelisk.HasValue
            && Exists(component.Obelisk.Value)
            && TryComp<NecroobeliskComponent>(component.Obelisk.Value, out var obeliskComp)
            && !obeliskComp.IsStageConvergence)
        {
            obeliskComp.IsStageConvergence = true;
            component.State = CircleOpsState.Convergence;
        }
    }

    private void OnRuleLoadedGrids(Entity<CircleOpsRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var query = EntityQueryEnumerator<CirclePrimaryShuttleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (Transform(uid).MapID == args.Map)
            {
                ent.Comp.Shuttle = uid;
                break;
            }
        }
    }

    private void OnStageObelisk(Entity<CircleOpsRuleComponent> ent, ref StageObeliskEvent args)
    {
        ent.Comp.Obelisk = args.Obelisk;
        _timedWindow.Reset(ent.Comp.WindowUntilSpawnMoon);

        ent.Comp.State = CircleOpsState.ObeliskActivated;
    }

    private void OnSpawnNecroMoon(Entity<CircleOpsRuleComponent> ent, ref SpawnNecroMoonEvent args)
    {
        _roundEndSystem.EndRound(CountdownRoundEndTime);
    }

    private void OnWarDeclared(ref WarDeclaredEvent ev)
    {
        var query = EntityQueryEnumerator<CircleOpsRuleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timedWindow.IsExpired(component.WindowBefoteWarDeclaration)
                && component.State == CircleOpsState.Preparing)
            {
                component.State = CircleOpsState.WarReady;
            }

            if (TryComp<RuleGridsComponent>(uid, out var grids) && Transform(ev.DeclaratorEntity).MapID != grids.Map)
                continue;

            var newStatus = GetWarDeclaredStatus(component);
            ev.Status = newStatus;

            if (component.TargetStation == null
                || newStatus != WarConditionStatus.WarReady)
                continue;

            component.State = CircleOpsState.WarDeclared;
            _timedWindow.Reset(component.WindowAfterWarDeclare);

            if (component.Shuttle is { } shuttleUid &&
                TryComp<CirclePrimaryShuttleComponent>(shuttleUid, out var shuttle))
            {
                shuttle.UnlockAt = component.WindowAfterWarDeclare.Remaining;
                shuttle.TimerStarted = true;
                shuttle.Unlocked = false;
                Dirty(shuttleUid, shuttle);
            }

            _alertLevel.SetLevel(component.TargetStation.Value, AlertLevel, false, true, true);

            if (!TryComp<StationBankAccountComponent>(component.TargetStation, out var stationAccount))
                return;

            var addMoneyAfterWarDeclared = _ertResponseSystem.GetErtPrice(ErtTeam) + AdditionalSupport;

            _cargoSystem.UpdateBankAccount(
                                (component.TargetStation.Value, stationAccount),
                                addMoneyAfterWarDeclared,
                                Account
                            );
        }
    }

    private WarConditionStatus? GetWarDeclaredStatus(CircleOpsRuleComponent component)
    {
        if (component.State == CircleOpsState.Inactive)
            return null;

        if (!_timedWindow.IsExpired(component.WindowBefoteWarDeclaration))
            return null;

        switch (component.State)
        {
            case CircleOpsState.Preparing:
                return WarConditionStatus.NoWarSmallCrew;
            case CircleOpsState.WarReady:
                return WarConditionStatus.WarReady;
            case CircleOpsState.WarDeclared:
                return WarConditionStatus.YesWar;
            case CircleOpsState.ObeliskActivated:
                return WarConditionStatus.YesWar;
            default:
                return WarConditionStatus.NoWarSmallCrew;
        }
    }
}

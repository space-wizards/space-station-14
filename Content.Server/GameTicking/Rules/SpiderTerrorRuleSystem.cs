// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Server.DeadSpace.Nuke;
using Content.Server.Station.Systems;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Server.AlertLevel;
using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Content.Server.Communications;
using Content.Shared.Mobs.Systems;
using Content.Server.DeadSpace.Spiders.SpideRoyalGuard.Components;
using Content.Server.Voting.Managers;
using Content.Shared.Voting;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Content.Shared.Mobs.Components;
using Content.Shared.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.DeadSpace.Nuke;
using Robust.Shared.Prototypes;
using Content.Server.DeadSpace.ERT;
using Content.Shared.DeadSpace.ERT.Prototypes;
using Content.Server.Database;

namespace Content.Server.GameTicking.Rules;

public sealed class SpiderTerrorRuleSystem : GameRuleSystem<SpiderTerrorRuleComponent>
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly NukeCodeSendQueueSystem _nukeCodeQueue = default!; // DS14
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly ErtResponseSystem _ertResponseSystem = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    private static readonly ProtoId<ErtTeamPrototype> ErtTeam = "CburnSierra";
    private static readonly ProtoId<CargoAccountPrototype> Account = "Security";
    // Сумма пополнения баланса станции на стадии размножения
    private const int AdditionalSupport = 70000;
    private const float ProgressBreeding = 0.45f;
    private const float ProgressNukeCode = 0.7f;
    private bool _voteSend = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderTerrorRuleComponent, SpiderTerrorAttackStationEvent>(OnAttackStation);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Started(EntityUid uid, SpiderTerrorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        _voteSend = true;
        component.UpdateUtil = _timing.CurTime + component.UpdateDuration;
        component.TimeUtilStartRule = _timing.CurTime + component.DurationStartRule;
    }

    protected override void AppendAdminStatus(EntityUid uid,
        SpiderTerrorRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var lines = new List<string>
        {
            Loc.GetString("game-rule-admin-status-spider-kings", ("count", GetSpiderKings())),
        };

        foreach (var (station, _) in component.StationStages)
        {
            var (progress, spiders) = GetCaptureStationProgress(uid, station, component);
            lines.Add(Loc.GetString(
                "game-rule-admin-status-spider-station",
                ("station", ToPrettyString(station).Name ?? station.ToString()),
                ("progress", progress.ToString("P0")),
                ("spiders", spiders),
                ("breeding", GetActivityStatus(component.IsBreedingActive(station))),
                ("nuclear", GetActivityStatus(component.IsNuclearCodeActive(station)))));
        }

        args.AddSection(Loc.GetString("game-rule-admin-status-spider-title"), lines);
    }

    protected override void ActiveTick(EntityUid uid, SpiderTerrorRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (_timing.CurTime >= component.UpdateUtil)
            EvaluateStageProgress(uid);

        if (_timing.CurTime >= component.TimeUtilStartRule)
            StartRule(uid);

        return;
    }

    protected override void AppendRoundEndText(EntityUid uid, SpiderTerrorRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.StationStages.Count != 0)
        {
            foreach (var kvp in component.StationStages)
            {
                var stationUid = kvp.Key;

                if (IsSpiderTerrorVictory(uid, stationUid, component))
                {
                    args.AddLine(Loc.GetString("spider-terror-win")); // Тут можно добавить: захватили станцию (название станции), чтобы не было дублирования одного предложения.

                    // Статистика для дашборда
                    var winner = BiStatWinner.Antagonist;
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await _db.AddBiStatAsync("Пауки ужаса", winner, DateTime.UtcNow);
                        }
                        catch
                        {

                        }
                    });
                }
                else
                {
                    args.AddLine(Loc.GetString("spider-terror-loose"));

                    var winner = BiStatWinner.Crew;
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await _db.AddBiStatAsync("Пауки ужаса", winner, DateTime.UtcNow);
                        }
                        catch
                        {

                        }
                    });
                }
            }
        }
    }

    private bool IsSpiderTerrorVictory(EntityUid uid, EntityUid stationUid, SpiderTerrorRuleComponent component)
    {
        var (progress, _) = GetCaptureStationProgress(uid, stationUid, component);
        return progress >= 1f;
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        bool isCanCall = true;

        var queryRule = EntityQueryEnumerator<SpiderTerrorRuleComponent>();
        var component = new SpiderTerrorRuleComponent();

        while (queryRule.MoveNext(out var rule, out var ruleComp))
        {
            if (ruleComp.StationStages.Count != 0)
            {
                foreach (var kvp in ruleComp.StationStages)
                {
                    var stationUid = kvp.Key;
                    var stationStage = kvp.Value;

                    if (ruleComp.IsBreedingActive(stationUid))
                    {
                        isCanCall = false;
                        component = ruleComp;
                        break;
                    }
                }
            }
        }

        if (_timing.CurTime >= component.TimeUtilSendMessage)
            component.SendMessageConsole = true;

        if (!isCanCall)
        {
            if (component.SendMessageConsole)
            {
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-shuttle-cancelled"), playSound: true, colorOverride: Color.LightSeaGreen);
                component.SendMessageConsole = false;
                component.TimeUtilSendMessage = _timing.CurTime + component.DurationSendMessage;
            }
            ev.Cancelled = true;
        }
    }

    private void OnAttackStation(EntityUid uid, SpiderTerrorRuleComponent component, SpiderTerrorAttackStationEvent ev)
    {
        var station = ev.Station;

        if (!component.StationStages.ContainsKey(station))
        {
            var stages = SpiderTerrorStages.None;
            component.StationStages.Add(station, stages);
        }
    }

    private void EvaluateStageProgress(EntityUid uid, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        foreach (var kvp in component.StationStages)
        {
            var stationUid = kvp.Key;

            var (progress, _) = GetCaptureStationProgress(uid, stationUid);

            var spidersCount = GetSpiders(uid, stationUid, component);
            var peopleCount = GetPeople(uid, stationUid, component);

            // Применяем логику в зависимости от стадии и прогресса
            if (progress >= ProgressBreeding || (peopleCount != null && spidersCount != null && peopleCount / spidersCount < component.PeopleOnSpidersBreeding))
            {
                Breeding(uid, stationUid); // Стадия захвата
            }

            if (progress >= ProgressNukeCode || (peopleCount != null && spidersCount != null && peopleCount / spidersCount < component.PeopleOnSpidersNukeCode))
            {
                NuclearCode(uid, stationUid); // Стадия кодов
            }
        }

        component.UpdateUtil = _timing.CurTime + component.UpdateDuration;
    }

    private string GetActivityStatus(bool active)
    {
        return Loc.GetString(active
            ? "game-rule-admin-status-active"
            : "game-rule-admin-status-inactive");
    }
    private void StartRule(EntityUid uid, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        EntityUid? stationEgg = null;
        var queryEgg = EntityQueryEnumerator<EggComponent>();

        while (queryEgg.MoveNext(out var spiderEnt, out var eggComp))
        {
            var xform = Transform(spiderEnt);
            var station = _station.GetStationInMap(xform.MapID);

            if (station == null)
            {
                continue;
            }
            else
            {
                stationEgg = station;
                break;
            }
        }

        if (stationEgg == null)
            return;

        if (component.StationStages.TryGetValue(stationEgg.Value, out var stages))
        {
            if (stages != SpiderTerrorStages.None)
                return;
        }
    }

    private void Breeding(EntityUid uid, EntityUid station, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (GetSpiderKings() <= 0 && !_voteSend)
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-spider-kings"), playSound: true, colorOverride: Color.Green);
            _voteManager.CreateStandardVote(null, StandardVoteType.Restart);
            _voteSend = true;
        }

        if (component.IsBreedingActive(station))
            return;

        component.StartBreeding(station);

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-station-was-breeding"), playSound: true, colorOverride: Color.Red);
        _alertLevel.SetLevel(station, "sierra", true, true, true);

        if (!TryComp<StationBankAccountComponent>(station, out var stationAccount))
            return;

        var addMoneyAfterWarDeclared = _ertResponseSystem.GetErtPrice(ErtTeam) + AdditionalSupport;

        _cargoSystem.UpdateBankAccount(
                            (station, stationAccount),
                            addMoneyAfterWarDeclared,
                            Account
                        );
    }

    private int GetSpiderKings()
    {
        var count = 0;

        var query = EntityQueryEnumerator<SpiderKingComponent>();

        while (query.MoveNext(out var ent, out _))
        {
            if (!_mobState.IsDead(ent))
                count++;
        }

        return count;
    }

    private void NuclearCode(EntityUid uid, EntityUid station, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsNuclearCodeActive(station))
            return;

        component.SendNuclearCode(station);

        // DS14-Start: queue automatic nuke-code dispatches for admin review.
        _nukeCodeQueue.TryQueueAutomaticRequest(
            station,
            NukeCodeSendReasonIds.SpiderTerrorCritical,
            out _);
        // DS14-End
    }

    private (float progress, int spiderCount) GetCaptureStationProgress(EntityUid uid, EntityUid station, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return (0f, 0);

        var querySpider = EntityQueryEnumerator<SpiderTerrorComponent>();
        int spiderCount = 0;
        float? progress = null;

        while (querySpider.MoveNext(out var spiderEnt, out _))
        {
            var xform = Transform(spiderEnt);
            var spiderStation = _station.GetStationInMap(xform.MapID);

            if (station != spiderStation)
                continue;

            if (!_mobState.IsDead(spiderEnt))
                spiderCount++;

            if (!_mindSystem.TryGetMind(spiderEnt, out var mindId, out var mind))
                continue;

            if (mind == null)
                continue;

            foreach (var objId in mind.Objectives)
            {
                if (!HasComp<SpiderTerrorConditionComponent>(objId))
                    continue;

                var result = _objectives.GetProgress(objId, (mindId, mind));

                if (result != null)
                {
                    progress = result.Value;
                }
            }
        }

        return (progress ?? 0f, spiderCount);
    }

    private float? GetPeople(EntityUid uid, EntityUid stationUid, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        float result = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, ActorComponent, MobStateComponent>();


        while (query.MoveNext(out var ent, out _, out _, out var state))
        {
            var xform = Transform(ent);
            var station = _station.GetStationInMap(xform.MapID);

            if (!_mobState.IsDead(ent, state) && station == stationUid)
                result++;
        }

        return result;
    }

    private float? GetSpiders(EntityUid uid, EntityUid stationUid, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        float result = 0;
        var query = EntityQueryEnumerator<SpiderTerrorComponent, MobStateComponent>();

        while (query.MoveNext(out var ent, out _, out var state))
        {
            var xform = Transform(ent);
            var station = _station.GetStationInMap(xform.MapID);

            if (!_mobState.IsDead(ent, state) && station == stationUid)
                result++;
        }

        return result;
    }
}

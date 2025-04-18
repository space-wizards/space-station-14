// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Server.Nuke;
using Content.Server.Station.Components;
using Robust.Shared.Audio;
using Content.Shared.Audio;
using Content.Server.Station.Systems;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Server.AlertLevel;
using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Content.Server.Communications;
using Content.Shared.Mobs.Systems;
using Content.Server.Chat.Managers;
using Content.Server.DeadSpace.Spiders.SpideRoyalGuard.Components;
using Content.Server.Voting.Managers;
using Content.Shared.Voting;

namespace Content.Server.GameTicking.Rules;

public sealed class SpiderTerrorRuleSystem : GameRuleSystem<SpiderTerrorRuleComponent>
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly NukeCodePaperSystem _nukeCodePaper = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;

    private const int SpidersBreeding = 30;
    private const int SpidersNukeCode = 55;
    private const float ProgressBreeding = 0.45f;
    private const float ProgressNukeCode = 0.65f;
    private const float ProgressCaptureStation = 0.98f;

    private bool voteSend = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderTerrorRuleComponent, SpiderTerrorAttackStationEvent>(OnAttackStation);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Started(EntityUid uid, SpiderTerrorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        voteSend = true;
        component.UpdateUtil = _timing.CurTime + component.UpdateDuration;
        component.TimeUtilStartRule = _timing.CurTime + component.DurationStartRule;
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

                if (component.IsStationCaptureActive(stationUid))
                {
                    args.AddLine(Loc.GetString("spider-terror-win")); // Тут можно добавить: захватили станцию (название станции), чтобы не было дублирования одного предложения.
                }
                else
                {
                    args.AddLine(Loc.GetString("spider-terror-loose"));
                }
            }
        }
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

            var (progress, spiders) = GetCaptureStationProgress(uid, stationUid);
            var msgProgress = "Прогресс захвата станции: " + EntityManager.ToPrettyString(stationUid).Name + ", " + (progress * 100).ToString() + "%";
            var msgSpiders = "На станции: " + EntityManager.ToPrettyString(stationUid).Name + ", " + spiders.ToString() + " пауков.";
            var msgSpidersKing = "На станции: " + (GetSpiderKings()).ToString() + " живых королевских пауков.";
            _chatManager.SendAdminAnnouncement(msgProgress);
            _chatManager.SendAdminAnnouncement(msgSpiders);
            _chatManager.SendAdminAnnouncement(msgSpidersKing);

            if (component.IsErtSendMessage)
            {
                var msgErt = "На станции: " + (GetCburnCount()).ToString() + " живых оперативников от первоначальных: " + component.CburnCount.ToString();
                _chatManager.SendAdminAnnouncement(msgErt);
            }

            if (component.IsBreedingActive(stationUid) && _timing.CurTime <= component.TimeUtilErt)
            {
                var time = _timing.CurTime - component.TimeUtilErt;
                double seconds = Math.Abs(time.TotalSeconds);
                int roundedSeconds = (int)Math.Round(seconds);
                var msgTimeErt = "ОБР прибудет через: " + roundedSeconds.ToString() + " секунд.";
                _chatManager.SendAdminAnnouncement(msgTimeErt);
            }

            // Применяем логику в зависимости от стадии и прогресса
            if (progress >= ProgressBreeding || spiders >= SpidersBreeding)
            {
                Breeding(uid, stationUid); // Стадия захвата
            }

            if (progress >= ProgressNukeCode || spiders >= SpidersNukeCode)
            {
                NuclearCode(uid, stationUid); // Стадия кодов
            }

            if (progress >= ProgressCaptureStation)
            {
                Capture(uid, stationUid); // Стадия захвата станции
            }
        }

        component.UpdateUtil = _timing.CurTime + component.UpdateDuration;
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


        if (_timing.CurTime >= component.TimeUtilErtAnnouncement && !component.IsErtSendMessage && component.IsBreedingActive(station))
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-special-response-team"), playSound: true, colorOverride: Color.Green);
            component.IsErtSendMessage = true;
        }

        if (_timing.CurTime >= component.TimeUtilErt && !component.IsErtSend && component.IsBreedingActive(station))
        {
            GameTicker.AddGameRule("ShuttleCBURNSCST");
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-response-team-arrival"), playSound: true, colorOverride: Color.Green);
            component.CburnCount = GetCburnCount();
            component.IsErtSend = true;
        }

        if (component.IsErtSend)
        {
            if ((float)GetCburnCount() / (float)component.CburnCount <= 0.2f)
                Capture(uid, station);
        }

        if (GetSpiderKings() <= 0 && !voteSend)
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-spider-kings"), playSound: true, colorOverride: Color.Green);
            _voteManager.CreateStandardVote(null, StandardVoteType.Restart);
            voteSend = true;
        }

        if (component.IsBreedingActive(station))
            return;

        component.StartBreeding(station);

        component.TimeUtilErtAnnouncement = _timing.CurTime + component.DurationErtAnnouncement;
        component.TimeUtilErt = _timing.CurTime + component.DurationErt;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-station-was-breeding"), playSound: true, colorOverride: Color.Red);
        GameTicker.AddGameRule("GiftsSpiderTerror");
        _alertLevel.SetLevel(station, "sierra", true, true, true);
    }

    private int GetCburnCount()
    {
        var count = 0;

        var query = EntityQueryEnumerator<CburnStaffComponent>();

        while (query.MoveNext(out var ent, out _))
        {
            if (!_mobState.IsDead(ent))
                count++;
        }

        return count;
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

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-station-was-nuke"), playSound: true, colorOverride: Color.OrangeRed);

        _nukeCodePaper.SendNukeCodes(station);
    }

    private void Capture(EntityUid uid, EntityUid station, SpiderTerrorRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_timing.CurTime >= component.TimeUtilErtAnnouncement && !component.IsDeadSquadSend && component.IsStationCaptureActive(station))
        {
            component.IsDeadSquadSend = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("spider-terror-centcomm-announcement-station-was-capture"), playSound: true, colorOverride: Color.Green);

            if (TryComp<StationDataComponent>(station, out var data))
            {
                var msg = new GameGlobalSoundEvent(component.Sound, AudioParams.Default);
                var stationFilter = _station.GetInStation(data);
                stationFilter.AddPlayersByPvs(station, entityManager: EntityManager);
                RaiseNetworkEvent(msg, stationFilter);
            }
        }

        if (_timing.CurTime >= component.TimeUtilErtAnnouncement && !component.IsDeadSquadArrival && component.IsStationCaptureActive(station))
        {
            component.IsDeadSquadArrival = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-response-team-arrival"), playSound: true, colorOverride: Color.Green);
            GameTicker.AddGameRule("ShuttleCBURNSCST");
        }

        if (_timing.CurTime >= component.TimeUtilErtAnnouncement && !component.IsCodeEpsilon && component.IsStationCaptureActive(station))
        {
            _alertLevel.SetLevel(station, "epsilon", true, true, true);

            component.IsCodeEpsilon = true;
        }

        if (component.IsStationCaptureActive(station))
            return;

        component.CaptureStation(station);

        component.TimeUtilDeadSquadAnnouncement = _timing.CurTime + component.DurationDeadSquadAnnouncement;
        component.TimeUtilDeadSquadArrival = _timing.CurTime + component.DurationDeadSquadArrival + component.DurationDeadSquadAnnouncement;
        component.TimeUtilCodeEpsilon = _timing.CurTime + component.DurationCodeEpsilon + component.DurationDeadSquadAnnouncement + component.DurationDeadSquadArrival;

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

            foreach (var objId in mind.AllObjectives)
            {
                if (!HasComp<SpiderTerrorConditionsComponent>(objId))
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
}

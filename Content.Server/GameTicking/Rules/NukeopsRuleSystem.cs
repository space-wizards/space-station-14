using Content.Server.Antag;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Nuke;
using Content.Server.NukeOps;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nuke;
using Content.Shared.NukeOps;
using Content.Shared.Store;
using Content.Shared.Tag;
using Content.Shared.Zombies;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Store.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem<NukeopsRuleComponent>
{
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    [ValidatePrototypeId<CurrencyPrototype>]
    private const string TelecrystalCurrencyPrototype = "Telecrystal";

    [ValidatePrototypeId<TagPrototype>]
    private const string NukeOpsUplinkTagPrototype = "NukeOpsUplink";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<NukeDisarmSuccessEvent>(OnNukeDisarm);

        SubscribeLocalEvent<NukeOperativeComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<NukeOperativeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<NukeOperativeComponent, EntityZombifiedEvent>(OnOperativeZombified);

        SubscribeLocalEvent<ConsoleFTLAttemptEvent>(OnShuttleFTLAttempt);
        SubscribeLocalEvent<WarDeclaredEvent>(OnWarDeclared);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);

        SubscribeLocalEvent<NukeopsRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntSelected);
        SubscribeLocalEvent<NukeopsRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
    }

    protected override void Started(EntityUid uid, NukeopsRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        var eligible = new List<Entity<StationEventEligibleComponent, NpcFactionMemberComponent>>();
        var eligibleQuery = EntityQueryEnumerator<StationEventEligibleComponent, NpcFactionMemberComponent>();
        while (eligibleQuery.MoveNext(out var eligibleUid, out var eligibleComp, out var member))
        {
            if (!_npcFaction.IsFactionHostile(component.Faction, (eligibleUid, member)))
                continue;

            eligible.Add((eligibleUid, eligibleComp, member));
        }

        if (eligible.Count == 0)
            return;

        component.TargetStation = RobustRandom.Pick(eligible);
    }

    #region Event Handlers
    protected override void AppendRoundEndText(EntityUid uid, NukeopsRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var winText = Loc.GetString($"nukeops-{component.WinType.ToString().ToLower()}");
        args.AddLine(winText);

        foreach (var cond in component.WinConditions)
        {
            var text = Loc.GetString($"nukeops-cond-{cond.ToString().ToLower()}");
            args.AddLine(text);
        }

        args.AddLine(Loc.GetString("nukeops-list-start"));

        var antags =_antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("nukeops-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            if (ev.OwningStation != null)
            {
                if (ev.OwningStation == GetOutpost(uid))
                {
                    nukeops.WinConditions.Add(WinCondition.NukeExplodedOnNukieOutpost);
                    SetWinType((uid, nukeops), WinType.CrewMajor);
                    continue;
                }

                if (TryComp(nukeops.TargetStation, out StationDataComponent? data))
                {
                    var correctStation = false;
                    foreach (var grid in data.Grids)
                    {
                        if (grid != ev.OwningStation)
                        {
                            continue;
                        }

                        nukeops.WinConditions.Add(WinCondition.NukeExplodedOnCorrectStation);
                        SetWinType((uid, nukeops), WinType.OpsMajor);
                        correctStation = true;
                    }

                    if (correctStation)
                        continue;
                }

                nukeops.WinConditions.Add(WinCondition.NukeExplodedOnIncorrectLocation);
            }
            else
            {
                nukeops.WinConditions.Add(WinCondition.NukeExplodedOnIncorrectLocation);
            }

            _roundEndSystem.EndRound();
        }
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New is not GameRunLevel.PostRound)
            return;

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            OnRoundEnd((uid, nukeops));
        }
    }

    private void OnRoundEnd(Entity<NukeopsRuleComponent> ent)
    {
        // If the win condition was set to operative/crew major win, ignore.
        if (ent.Comp.WinType == WinType.OpsMajor || ent.Comp.WinType == WinType.CrewMajor)
            return;

        var nukeQuery = AllEntityQuery<NukeComponent, TransformComponent>();
        var centcomms = _emergency.GetCentcommMaps();

        while (nukeQuery.MoveNext(out var nuke, out var nukeTransform))
        {
            if (nuke.Status != NukeStatus.ARMED)
                continue;

            // UH OH
            if (nukeTransform.MapUid != null && centcomms.Contains(nukeTransform.MapUid.Value))
            {
                ent.Comp.WinConditions.Add(WinCondition.NukeActiveAtCentCom);
                SetWinType((ent, ent), WinType.OpsMajor);
                return;
            }

            if (nukeTransform.GridUid == null || ent.Comp.TargetStation == null)
                continue;

            if (!TryComp(ent.Comp.TargetStation.Value, out StationDataComponent? data))
                continue;

            foreach (var grid in data.Grids)
            {
                if (grid != nukeTransform.GridUid)
                    continue;

                ent.Comp.WinConditions.Add(WinCondition.NukeActiveInStation);
                SetWinType(ent, WinType.OpsMajor);
                return;
            }
        }

        if (_antag.AllAntagsAlive(ent.Owner))
        {
            SetWinType(ent, WinType.OpsMinor);
            ent.Comp.WinConditions.Add(WinCondition.AllNukiesAlive);
            return;
        }

        ent.Comp.WinConditions.Add(_antag.AnyAliveAntags(ent.Owner)
            ? WinCondition.SomeNukiesAlive
            : WinCondition.AllNukiesDead);

        var diskAtCentCom = false;
        var diskQuery = AllEntityQuery<NukeDiskComponent, TransformComponent>();
        while (diskQuery.MoveNext(out _, out var transform))
        {
            diskAtCentCom = transform.MapUid != null && centcomms.Contains(transform.MapUid.Value);

            // TODO: The target station should be stored, and the nuke disk should store its original station.
            // This is fine for now, because we can assume a single station in base SS14.
            break;
        }

        // If the disk is currently at Central Command, the crew wins - just slightly.
        // This also implies that some nuclear operatives have died.
        SetWinType(ent, diskAtCentCom
            ? WinType.CrewMinor
            : WinType.OpsMinor);
        ent.Comp.WinConditions.Add(diskAtCentCom
            ? WinCondition.NukeDiskOnCentCom
            : WinCondition.NukeDiskNotOnCentCom);
    }

    private void OnNukeDisarm(NukeDisarmSuccessEvent ev)
    {
        CheckRoundShouldEnd();
    }

    private void OnComponentRemove(EntityUid uid, NukeOperativeComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnMobStateChanged(EntityUid uid, NukeOperativeComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnOperativeZombified(EntityUid uid, NukeOperativeComponent component, ref EntityZombifiedEvent args)
    {
        RemCompDeferred(uid, component);
    }

    private void OnRuleLoadedGrids(Entity<NukeopsRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        // Check each nukie shuttle
        var query = EntityQueryEnumerator<NukeOpsShuttleComponent>();
        while (query.MoveNext(out var uid, out var shuttle))
        {
            // Check if the shuttle's mapID is the one that just got loaded for this rule
            if (Transform(uid).MapID == args.Map)
            {
                shuttle.AssociatedRule = ent;
                break;
            }
        }
    }

    private void OnShuttleFTLAttempt(ref ConsoleFTLAttemptEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            if (ev.Uid != GetShuttle((uid, nukeops)))
                continue;

            if (nukeops.WarDeclaredTime != null)
            {
                var timeAfterDeclaration = Timing.CurTime.Subtract(nukeops.WarDeclaredTime.Value);
                var timeRemain = nukeops.WarNukieArriveDelay.Subtract(timeAfterDeclaration);
                if (timeRemain > TimeSpan.Zero)
                {
                    ev.Cancelled = true;
                    ev.Reason = Loc.GetString("war-ops-infiltrator-unavailable",
                        ("time", timeRemain.ToString("mm\\:ss")));
                    continue;
                }
            }

            nukeops.LeftOutpost = true;
        }
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var nukeops, out _))
        {
            // Can't call while war nukies are preparing to arrive
            if (nukeops is { WarDeclaredTime: not null })
            {
                // Nukies must wait some time after declaration of war to get on the station
                var warTime = Timing.CurTime.Subtract(nukeops.WarDeclaredTime.Value);
                if (warTime < nukeops.WarNukieArriveDelay)
                {
                    ev.Cancelled = true;
                    ev.Reason = Loc.GetString("war-ops-shuttle-call-unavailable");
                    return;
                }
            }
        }
    }

    private void OnWarDeclared(ref WarDeclaredEvent ev)
    {
        // TODO: this is VERY awful for multi-nukies
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            if (nukeops.WarDeclaredTime != null)
                continue;

            if (TryComp<RuleGridsComponent>(uid, out var grids) && Transform(ev.DeclaratorEntity).MapID != grids.Map)
                continue;

            var newStatus = GetWarCondition(nukeops, ev.Status);
            ev.Status = newStatus;
            if (newStatus == WarConditionStatus.WarReady)
            {
                nukeops.WarDeclaredTime = Timing.CurTime;
                var timeRemain = nukeops.WarNukieArriveDelay + Timing.CurTime;
                ev.DeclaratorEntity.Comp.ShuttleDisabledTime = timeRemain;

                DistributeExtraTc((uid, nukeops));
            }
        }
    }

    #endregion Event Handlers

    /// <summary>
    ///     Returns conditions for war declaration
    /// </summary>
    public WarConditionStatus GetWarCondition(NukeopsRuleComponent nukieRule, WarConditionStatus? oldStatus)
    {
        if (!nukieRule.CanEnableWarOps)
            return WarConditionStatus.NoWarUnknown;

        if (EntityQuery<NukeopsRoleComponent>().Count() < nukieRule.WarDeclarationMinOps)
            return WarConditionStatus.NoWarSmallCrew;

        if (nukieRule.LeftOutpost)
            return WarConditionStatus.NoWarShuttleDeparted;

        if (oldStatus == WarConditionStatus.YesWar)
            return WarConditionStatus.WarReady;

        return WarConditionStatus.YesWar;
    }

    private void DistributeExtraTc(Entity<NukeopsRuleComponent> nukieRule)
    {
        var enumerator = EntityQueryEnumerator<StoreComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (!_tag.HasTag(uid, NukeOpsUplinkTagPrototype))
                continue;

            if (GetOutpost(nukieRule.Owner) is not { } outpost)
                continue;

            if (Transform(uid).MapID != Transform(outpost).MapID) // Will receive bonus TC only on their start outpost
                continue;

            _store.TryAddCurrency(new() { { TelecrystalCurrencyPrototype, nukieRule.Comp.WarTcAmountPerNukie } }, uid, component);

            var msg = Loc.GetString("store-currency-war-boost-given", ("target", uid));
            _popupSystem.PopupEntity(msg, uid);
        }
    }

    private void SetWinType(Entity<NukeopsRuleComponent> ent, WinType type, bool endRound = true)
    {
        ent.Comp.WinType = type;

        if (endRound && (type == WinType.CrewMajor || type == WinType.OpsMajor))
            _roundEndSystem.EndRound();
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            CheckRoundShouldEnd((uid, nukeops));
        }
    }

    private void CheckRoundShouldEnd(Entity<NukeopsRuleComponent> ent)
    {
        var nukeops = ent.Comp;

        if (nukeops.RoundEndBehavior == RoundEndBehavior.Nothing || nukeops.WinType == WinType.CrewMajor || nukeops.WinType == WinType.OpsMajor)
            return;


        // If there are any nuclear bombs that are active, immediately return. We're not over yet.
        foreach (var nuke in EntityQuery<NukeComponent>())
        {
            if (nuke.Status == NukeStatus.ARMED)
                return;
        }

        var shuttle = GetShuttle((ent, ent));

        MapId? shuttleMapId = Exists(shuttle)
            ? Transform(shuttle.Value).MapID
            : null;

        MapId? targetStationMap = null;
        if (nukeops.TargetStation != null && TryComp(nukeops.TargetStation, out StationDataComponent? data))
        {
            var grid = data.Grids.FirstOrNull();
            targetStationMap = grid != null
                ? Transform(grid.Value).MapID
                : null;
        }

        // Check if there are nuke operatives still alive on the same map as the shuttle,
        // or on the same map as the station.
        // If there are, the round can continue.
        var operatives = EntityQuery<NukeOperativeComponent, MobStateComponent, TransformComponent>(true);
        var operativesAlive = operatives
            .Where(op =>
                op.Item3.MapID == shuttleMapId
                || op.Item3.MapID == targetStationMap)
            .Any(op => op.Item2.CurrentState == MobState.Alive && op.Item1.Running);

        if (operativesAlive)
            return; // There are living operatives than can access the shuttle, or are still on the station's map.

        // Check that there are spawns available and that they can access the shuttle.
        var spawnsAvailable = EntityQuery<NukeOperativeSpawnerComponent>(true).Any();
        if (spawnsAvailable && CompOrNull<RuleGridsComponent>(ent)?.Map == shuttleMapId)
            return; // Ghost spawns can still access the shuttle. Continue the round.

        // The shuttle is inaccessible to both living nuke operatives and yet to spawn nuke operatives,
        // and there are no nuclear operatives on the target station's map.
        nukeops.WinConditions.Add(spawnsAvailable
            ? WinCondition.NukiesAbandoned
            : WinCondition.AllNukiesDead);

        SetWinType(ent, WinType.CrewMajor, false);
        _roundEndSystem.DoRoundEndBehavior(
            nukeops.RoundEndBehavior, nukeops.EvacShuttleTime, nukeops.RoundEndTextSender, nukeops.RoundEndTextShuttleCall, nukeops.RoundEndTextAnnouncement);

        // prevent it called multiple times
        nukeops.RoundEndBehavior = RoundEndBehavior.Nothing;
    }

    private void OnAfterAntagEntSelected(Entity<NukeopsRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (ent.Comp.TargetStation is not { } station)
            return;

        _antag.SendBriefing(args.Session, Loc.GetString("nukeops-welcome",
                ("station", station),
                ("name", Name(ent))),
            Color.Red,
            ent.Comp.GreetSoundNotification);
    }

    /// <remarks>
    /// Is this method the shitty glue holding together the last of my sanity? yes.
    /// Do i have a better solution? not presently.
    /// </remarks>
    private EntityUid? GetOutpost(Entity<RuleGridsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        return ent.Comp.MapGrids.Where(e => !HasComp<NukeOpsShuttleComponent>(e)).FirstOrNull();
    }

    /// <remarks>
    /// Is this method the shitty glue holding together the last of my sanity? yes.
    /// Do i have a better solution? not presently.
    /// </remarks>
    private EntityUid? GetShuttle(Entity<NukeopsRuleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        var query = EntityQueryEnumerator<NukeOpsShuttleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AssociatedRule == ent.Owner)
                return uid;
        }

        return null;
    }
}

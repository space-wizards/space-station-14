using System.Linq;
using System.Text;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Nuke;
using Content.Server.NukeOps;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Rules;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nuke;
using Content.Shared.NukeOps;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.RoundEnd;
using Content.Shared.Station.Components;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Systems;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerNukeopsRuleSystem : NukeopsRuleSystem
{
    [Dependency] private EmergencyShuttleSystem _emergency = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<CurrencyPrototype> TelecrystalCurrencyPrototype = "Telecrystal";
    private static readonly ProtoId<TagPrototype> NukeOpsUplinkTagPrototype = "NukeOpsUplink";

    #region Event Handlers
    protected override void AppendRoundEndText(Entity<NukeopsRuleComponent> rule,
        ref RoundEndTextAppendEvent args)
    {
        var winText = Loc.GetString($"nukeops-{rule.Comp.WinType.ToString().ToLower()}");
        args.AddLine(winText);

        foreach (var cond in rule.Comp.WinConditions)
        {
            var text = Loc.GetString($"nukeops-cond-{cond.ToString().ToLower()}");
            args.AddLine(text);
        }

        // Print disk location if nuke didn't explode and is not armed
        List<WinCondition> diskWinConditions = [WinCondition.NukeDiskOnCentCom, WinCondition.NukeDiskNotOnCentCom];
        if (rule.Comp.WinConditions.Any(diskWinConditions.Contains))
        {
            var diskQuery = AllEntityQuery<NukeDiskComponent, TransformComponent>();
            while (diskQuery.MoveNext(out var diskUid, out _, out var transform))
            {
                StringBuilder text = new StringBuilder(Loc.GetString("nukeops-disk-location-title"));

                List<String> containers = new List<String>();
                bool carriedByMob = false;

                var tempParent = diskUid;
                while (_containers.TryGetContainingContainer((tempParent, null), out var container) && !carriedByMob)
                {
                    if (HasComp<MindContainerComponent>(container.Owner))
                    {
                        carriedByMob = true;
                    }
                    var containermeta = MetaData(container.Owner);
                    containers.Add(containermeta.EntityName);
                    tempParent = container.Owner;
                }

                string location = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((diskUid, transform)));

                if (carriedByMob)
                {
                    GetDiskCarrierData(tempParent, out var name, out var job, out var username);
                    text.Append(Loc.GetString("nukeops-disk-carried-by",
                        ("name", name),
                        ("job", job),
                        ("user", username),
                        ("location", location)));
                }
                else
                {
                    if (containers.Count > 0)
                    {
                        string hierarchy = string.Empty;
                        for (var i = 0; i < containers.Count; i++)
                        {
                            hierarchy = (Loc.GetString(
                                "storage-hierarchy-list",
                                ("item", containers[i]),
                                ("existing-text", hierarchy),
                                ("items-left", containers.Count - i - 1)));
                        }
                        text.Append(hierarchy);
                    }
                    text.Append(" ");
                    text.Append(location);
                }
                args.AddLine(text.ToString());
            }
        }

        args.AddLine(Loc.GetString("nukeops-list-start"));

        var antags = Antag.GetAntagIdentifiers(rule.Owner);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("nukeops-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
        args.AddLine("");
    }

    [SubscribeLocalEvent]
    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        // TODO: Why are we querying the active rules for NukeOps, then within EACH UID checking if the NukeOpsGameRule exists???
        // TODO: O(N^2) Operations ass.
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var nukeops, out _))
        {
            if (ev.OwningStation != null)
            {
                if (ev.OwningStation == GetOutpost(uid))
                {
                    nukeops.WinConditions.Add(WinCondition.NukeExplodedOnNukieOutpost);
                    SetWinType((uid, nukeops), WinType.CrewMajor, GameTicker.IsGameRuleActive(NukeopsGameRule)); // End the round ONLY if the actual gamemode is NukeOps.
                    if (!GameTicker.IsGameRuleActive(NukeopsGameRule)) // End the rule if the LoneOp shuttle got nuked, because that particular LoneOp clearly failed, and should not be considered a Syndie victory even if a future LoneOp wins.
                        GameTicker.EndGameRule(uid);
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

            if (GameTicker.IsGameRuleActive(NukeopsGameRule)) // If it's Nukeops then end the round on any detonation
            {
                _roundEndSystem.EndRound();
            }
            else
            {
                // It's a LoneOp. Only end the round if the station was destroyed
                var handled = false;
                foreach (var cond in nukeops.WinConditions)
                {
                    if (cond.ToString().ToLower() == "NukeExplodedOnCorrectStation") // If this is true, then the nuke destroyed the station! It's likely everyone is very dead so keeping the round going is pointless.
                    {
                        _roundEndSystem.EndRound(); // end the round!
                        handled = true;
                        break;
                    }
                }
                if (!handled) // The round didn't end, so end the rule so it doesn't get overridden by future LoneOps.
                {
                    GameTicker.EndGameRule(uid);
                }
            }
        }
    }

    [SubscribeLocalEvent]
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

        if (Antag.AllAntagsAlive(ent.Owner))
        {
            ent.Comp.WinConditions.Add(WinCondition.AllNukiesAlive);
        }
        else
        {
            ent.Comp.WinConditions.Add(Antag.AnyAliveAntags(ent.Owner)
                ? WinCondition.SomeNukiesAlive
                : WinCondition.AllNukiesDead);
        }

        var diskAtCentCom = false;
        var diskQuery = AllEntityQuery<NukeDiskComponent, TransformComponent>();
        while (diskQuery.MoveNext(out var diskUid, out _, out var transform))
        {
            diskAtCentCom = transform.MapUid != null && centcomms.Contains(transform.MapUid.Value);
            diskAtCentCom |= _emergency.IsTargetEscaping(diskUid);

            // TODO: The target station should be stored, and the nuke disk should store its original station.
            // This is fine for now, because we can assume a single station in base SS14.
            break;
        }

        // If the disk is currently at Central Command, the crew wins - just slightly.
        SetWinType(ent,
            diskAtCentCom
            ? WinType.CrewMinor
            : WinType.OpsMinor);
        ent.Comp.WinConditions.Add(diskAtCentCom
            ? WinCondition.NukeDiskOnCentCom
            : WinCondition.NukeDiskNotOnCentCom);
    }

    [SubscribeLocalEvent]
    private void OnNukeDisarm(NukeDisarmSuccessEvent ev)
    {
        CheckRoundShouldEnd();
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(Entity<NukeOperativeComponent> entity, ref ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<NukeOperativeComponent> entity, ref MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    [SubscribeLocalEvent]
    private void OnOperativeZombified(Entity<NukeOperativeComponent> entity, ref EntityZombifiedEvent args)
    {
        RemCompDeferred(entity, entity.Comp);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
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
                if (warTime < nukeops.WarEvacShuttleDisabled)
                {
                    ev.Cancelled = true;
                    ev.Reason = Loc.GetString("war-ops-shuttle-call-unavailable");
                    return;
                }
            }
        }
    }

    [SubscribeLocalEvent]
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

        if (nukeops.WinType == WinType.CrewMajor || nukeops.WinType == WinType.OpsMajor) // Skip this if the round's victor has already been decided.
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

        if (nukeops.RoundEndBehavior == RoundEndBehavior.Nothing) // It's still worth checking if operatives have all died, even if the round-end behaviour is nothing.
            return; // Shouldn't actually try to end the round in the case of nothing though.

        _roundEndSystem.DoRoundEndBehavior(nukeops.RoundEndBehavior,
        nukeops.EvacShuttleTime,
        nukeops.RoundEndTextSender,
        nukeops.RoundEndTextShuttleCall,
        nukeops.RoundEndTextAnnouncement);


        // prevent it called multiple times
        nukeops.RoundEndBehavior = RoundEndBehavior.Nothing;
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

    private void GetDiskCarrierData(EntityUid carrier,
        out string name,
        out string job,
        out string username)
    {
        name = Name(carrier);
        job = Loc.GetString("job-name-unknown");
        username = "unknown"; // magic word in Fluent selector

        Entity<MindComponent>? mind = null;

        if (_mind.TryGetMind(carrier, out _, out var mindComp))
        {
            mind = (carrier, mindComp);
        }
        else
        {
            var allMinds = EntityQueryEnumerator<MindComponent>();
            while (allMinds.MoveNext(out _, out mindComp))
            {
                if (mindComp.CharacterName != name)
                    continue;

                mind = (carrier, mindComp);
                break;
            }
        }

        if (mind is not null)
        {
            NetUserId? userId = mind.Value.Comp.UserId;
            if (userId is not null && _player.TryGetPlayerData(userId.Value, out var sessionData))
                username = sessionData.UserName;

            // Role/job is the trickiest since it can be unknown in some cases
            // For example, after "make ghost role" verb
            var roles = _roles.MindGetAllRoleInfo(mind.Value.Owner);
            if (roles.Count > 0)
            {
                job = Loc.GetString(roles.First().Name);
                return;
            }

            if (_jobs.MindTryGetJobName(mind, out var jobName))
            {
                job = jobName;
                return;
            }
        }

        // Try station records
        var xform = Transform(carrier);
        var station = _station.GetStationInMap(xform.MapID);
        if (station != null && _records.GetRecordByName(station.Value, name) is { } id)
        {
            var key = new StationRecordKey(id, station.Value);
            if (_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                job = record.JobTitle;
                return;
            }
        }

        // Fallback to ID
        if (_idCard.TryFindIdCard(carrier, out var idCard))
            job = idCard.Comp.LocalizedJobTitle ?? job;
    }
}

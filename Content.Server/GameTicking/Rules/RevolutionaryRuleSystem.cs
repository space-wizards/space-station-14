// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Server.DeadSpace.ERT;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Revolutionary;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.DeadSpace.ERT.Prototypes;
using Content.Shared.Flash;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.StatusIcon;
using Content.Shared.Stunnable;
using Content.Shared.Voting;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assigns revolutionaries and keeps their active state on minds instead of historical bodies.
/// Icon visibility is synchronized with targeted roster messages rather than session-specific components.
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ErtResponseSystem _ertResponseSystem = default!;

    public readonly ProtoId<ErtTeamPrototype> RevolutionarySupplyTeam = "RevSup";
    public readonly EntProtoId Objective = "KillCommandStaffObjective";
    public readonly ProtoId<NpcFactionPrototype> RevolutionaryNpcFaction = "Revolutionary";

    private readonly HashSet<EntityUid> _visibleRevolutionaries = new();
    private readonly HashSet<EntityUid> _visibleHeadRevolutionaries = new();
    private readonly Dictionary<NetEntity, ProtoId<FactionIconPrototype>> _pendingAddedRevolutionaries = new();
    private readonly HashSet<NetEntity> _pendingRemovedRevolutionaries = new();
    private readonly Dictionary<NetEntity, ProtoId<FactionIconPrototype>> _pendingAddedHeadRevolutionaries = new();
    private readonly HashSet<NetEntity> _pendingRemovedHeadRevolutionaries = new();
    private readonly HashSet<ICommonSession> _rosterViewers = new();
    private readonly HashSet<ICommonSession> _pendingRosterSnapshots = new();
    private readonly HashSet<ICommonSession> _pendingRosterClears = new();
    private RevolutionaryRosterSyncEvent? _cachedRosterSnapshot;
    private EntityUid? _cachedRule;
    private EntityUid? _pendingCleanupRule;

    internal int RosterDeltaBatchCount { get; private set; }
    internal int RosterSnapshotBuildCount { get; private set; }
    internal int RosterSnapshotBatchCount { get; private set; }
    internal int RosterSnapshotSendCount { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, ComponentStartup>(OnRevolutionaryStartup);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentShutdown>(OnRevolutionaryShutdown);
        SubscribeLocalEvent<RevolutionaryComponent, MobStateChangedEvent>(OnRevolutionaryMobStateChanged);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentStartup>(OnHeadRevolutionaryStartup);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentShutdown>(OnHeadRevolutionaryShutdown);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevolutionaryMobStateChanged);
        SubscribeLocalEvent<CommandStaffComponent, ComponentStartup>(OnCommandStartup);
        SubscribeLocalEvent<CommandStaffComponent, ComponentShutdown>(OnCommandShutdown);
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);

        SubscribeLocalEvent<RevolutionaryComponent, MindAddedMessage>(OnRevolutionaryMindAdded);
        SubscribeLocalEvent<RevolutionaryComponent, MindRemovedMessage>(OnRevolutionaryMindRemoved);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MindAddedMessage>(OnHeadMindAdded);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MindRemovedMessage>(OnHeadMindRemoved);
        SubscribeLocalEvent<CommandStaffComponent, MindAddedMessage>(OnCommandMindAdded);
        SubscribeLocalEvent<CommandStaffComponent, MindRemovedMessage>(OnCommandMindRemoved);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentStartup>(OnShowAntagIconsStartup);
        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentShutdown>(OnShowAntagIconsShutdown);

        SubscribeLocalEvent<HeadRevolutionaryComponent, HeadRevConvertActionEvent>(OnTargetWithConvertWindow);
        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MapInitEvent>(OnHeadMapInit);
        SubscribeLocalEvent<RevolutionaryRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    protected override void Started(
        EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _cachedRule = uid;
        ResetRuntimeState(component);
        RebuildRuleState(component);
        RebuildVisibleRoster();
        component.Check = _timing.CurTime + component.TimerWait;
    }

    protected override void ActiveTick(
        EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.HeadCheckPending)
        {
            component.HeadCheckPending = false;
            if (component.HadHeadRevolutionaries &&
                component.HeadRevolutionaryMinds.Count == 0 &&
                !component.DefeatHandled)
            {
                BeginDefeat(uid, component, gameRule);
                return;
            }
        }

        var periodicCheck = component.Check <= _timing.CurTime;
        if (!periodicCheck && !component.ProgressCheckPending)
            return;

        if (periodicCheck)
            component.Check = _timing.CurTime + component.TimerWait;
        component.ProgressCheckPending = false;

        if (component.HadHeadRevolutionaries &&
            component.HeadRevolutionaryMinds.Count == 0 &&
            !component.DefeatHandled)
        {
            BeginDefeat(uid, component, gameRule);
            return;
        }

        if (component.HeadRevolutionaryMinds.Count == 0)
            return;

        if (component.CommandDeadFraction >= component.MassacreCommandFraction &&
            component.Stage == RevolutionaryStage.Initial)
        {
            BeginMassacre(uid, component);
        }

        if (component.CommandDeadFraction < component.VictoryCommandFraction ||
            component.VoteStarted ||
            !CanAutomaticallyStartRoundEndVote())
        {
            return;
        }

        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("rev-alert-stage-massacre-end-with-rev-won"),
            colorOverride: Color.Red,
            usePresetTTS: true);
        _voteManager.CreateStandardVote(null, StandardVoteType.Restart);
        component.VoteStarted = true;
    }

    /// <summary>
    /// Automatic voting is only a fallback for rounds without an active administrator
    /// who has permission to end the round.
    /// </summary>
    internal bool CanAutomaticallyStartRoundEndVote()
    {
        foreach (var admin in _admin.ActiveAdmins)
        {
            if (_admin.HasAdminFlag(admin, AdminFlags.Round))
                return false;
        }

        return true;
    }

    protected override void AppendAdminStatus(EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var stationHeads = 0;
        var stationRevolutionaries = 0;
        var lines = new List<string>();
        var healthy = GetHealthyHumanoids();

        foreach (var (mindId, body) in component.RevolutionaryMinds)
        {
            if (!healthy.Contains(body))
                continue;

            if (component.HeadRevolutionaryMinds.Contains(mindId))
                stationHeads++;
            else
                stationRevolutionaries++;
        }

        var objectiveProgress = GetObjectiveProgress(component, component.VictoryCommandFraction);
        foreach (var mindId in component.HeadRevolutionaryMinds)
        {
            if (!component.RevolutionaryMinds.TryGetValue(mindId, out var head) ||
                !TryComp<MindComponent>(mindId, out var mind))
            {
                continue;
            }

            foreach (var objective in mind.Objectives)
            {
                if (!Exists(objective))
                    continue;

                var progress = MetaData(objective).EntityPrototype?.ID == Objective.Id
                    ? objectiveProgress.ToString("P0")
                    : Loc.GetString("game-rule-admin-status-unknown");
                lines.Add(Loc.GetString("game-rule-admin-status-revolution-objective",
                    ("head", ToPrettyString(head).Name ?? head.ToString()),
                    ("objective", MetaData(objective).EntityName),
                    ("progress", progress)));
            }
        }

        var healthyCount = healthy.Count;
        var heads = component.HeadRevolutionaryMinds.Count;
        var revolutionaries = Math.Max(component.RevolutionaryMinds.Count - heads, 0);
        var fraction = healthyCount == 0
            ? 0f
            : Math.Clamp((stationHeads + stationRevolutionaries) / (float) healthyCount, 0f, 1f);
        var stage = component.VoteStarted
            ? "vote"
            : component.Stage.ToString().ToLowerInvariant();

        lines.Insert(0, Loc.GetString("game-rule-admin-status-revolution-summary",
            ("stage", Loc.GetString($"game-rule-admin-status-revolution-stage-{stage}")),
            ("heads", heads),
            ("revolutionaries", revolutionaries),
            ("healthy", healthyCount),
            ("fraction", fraction.ToString("P0"))));

        if (lines.Count == 1)
            lines.Add(Loc.GetString("game-rule-admin-status-revolution-no-objectives"));

        args.AddSection(Loc.GetString("game-rule-admin-status-revolution-title"), lines);
    }

    /// <summary>
    /// Status reports are collected once per minute. This is the only remaining full player scan;
    /// the gameplay checks use the event-driven rule rosters instead.
    /// </summary>
    private HashSet<EntityUid> GetHealthyHumanoids()
    {
        var humanoids = new HashSet<EntityUid>();
        var stationGrids = new HashSet<EntityUid>();

        foreach (var station in _stationSystem.GetStationsSet())
        {
            if (_stationSystem.GetLargestGrid(station) is { } grid)
                stationGrids.Add(grid);
        }

        var players =
            AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, MobStateComponent, TransformComponent>();
        while (players.MoveNext(out var uid, out _, out _, out var mob, out var xform))
        {
            if (_mobState.IsAlive(uid, mob) &&
                stationGrids.Contains(xform.GridUid ?? EntityUid.Invalid))
            {
                humanoids.Add(uid);
            }
        }

        return humanoids;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Defeat ends the rule immediately, so queued cleanup must continue independently of ActiveTick.
        if (_pendingCleanupRule is { } rule &&
            TryComp<RevolutionaryRuleComponent>(rule, out var component))
        {
            var count = Math.Max(component.DeconversionBatchSize, 1);
            while (count-- > 0 && component.PendingDeconversions.TryDequeue(out var body))
            {
                component.PendingDeconversionSet.Remove(body);
                if (!Exists(body) || HasComp<HeadRevolutionaryComponent>(body))
                    continue;

                Deconvert(
                    body,
                    stun: true,
                    showPopup: true,
                    showEui: true,
                    "all Head Revolutionaries died");
            }

            if (component.PendingDeconversions.Count == 0)
                _pendingCleanupRule = null;
        }
        else
            _pendingCleanupRule = null;

        FlushRosterChanges();
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine(Loc.GetString(
            "rev-objectives-progress",
            ("progress", GetObjectiveProgress(component, component.VictoryCommandFraction).ToString("P0"))));

        var sessionData = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("rev-headrev-count", ("initialCount", sessionData.Count)));
        foreach (var (mind, data, name) in sessionData)
        {
            var count = 0u;
            if (_role.MindHasRole<RevolutionaryRoleComponent>(mind, out var role))
                count = role.Value.Comp2.ConvertedCount;

            args.AddLine(Loc.GetString(
                "rev-headrev-name-user",
                ("name", name),
                ("username", data.UserName),
                ("count", count)));
        }

        args.AddLine("");

        var winner = component.CommandDeadFraction >= component.VictoryCommandFraction
            ? BiStatWinner.Antagonist
            : BiStatWinner.Crew;
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await _db.AddBiStatAsync("Революция", winner, DateTime.UtcNow);
            }
            catch
            {
                // Round-end statistics must not interrupt round cleanup.
            }
        });
    }

    /// <summary>
    /// Cached objective progress. This is O(1); the command roster is refreshed by lifecycle events.
    /// </summary>
    public float GetCommandObjectiveProgress(int targetPercent)
    {
        if (targetPercent <= 0)
            return 1f;

        return TryGetRule(out var rule)
            ? Math.Min(rule.Comp.CommandDeadFraction * 100f / targetPercent, 1f)
            : 0f;
    }

    public bool TryGetRuleState(out Entity<RevolutionaryRuleComponent> rule)
    {
        return TryGetRule(out rule);
    }

    public bool TryGetActiveRevolutionaryBody(EntityUid mindId, out EntityUid body)
    {
        body = default;
        return TryGetRule(out var rule) &&
               rule.Comp.RevolutionaryMinds.TryGetValue(mindId, out body);
    }

    public bool IsActiveRevolutionaryBody(EntityUid body)
    {
        return TryGetRule(out var rule) &&
               rule.Comp.RevolutionaryBodies.ContainsKey(body);
    }

    public bool TryGetTrackedCommandBody(EntityUid mindId, out EntityUid body)
    {
        body = default;
        return TryGetRule(out var rule) &&
               rule.Comp.CommandMinds.TryGetValue(mindId, out body);
    }

    public bool IsTrackedCommandBody(EntityUid body)
    {
        return TryGetRule(out var rule) &&
               rule.Comp.CommandBodies.ContainsKey(body);
    }

    private void BeginMassacre(EntityUid uid, RevolutionaryRuleComponent component)
    {
        component.Stage = RevolutionaryStage.Massacre;

        var headRevNames = _antag.GetAntagIdentifiers(uid)
            .Select(entry => entry.Item3)
            .ToList();
        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString(
                "rev-alert-stage-massacre-start",
                ("headRevsNames", string.Join(", ", headRevNames))),
            colorOverride: Color.Red,
            usePresetTTS: true);

        if (component.SupplyRequested)
            return;

        component.SupplyRequested = true;
        _ertResponseSystem.TryCallErt(
            RevolutionarySupplyTeam,
            null,
            out _,
            false,
            false,
            false,
            "Доставить вооружение революционерам",
            null);
    }

    private void BeginDefeat(
        EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule)
    {
        if (component.DefeatHandled)
            return;

        component.DefeatHandled = true;

        var revQuery = EntityQueryEnumerator<RevolutionaryComponent>();
        while (revQuery.MoveNext(out var body, out _))
        {
            if (HasComp<HeadRevolutionaryComponent>(body) ||
                !component.PendingDeconversionSet.Add(body))
            {
                continue;
            }

            component.PendingDeconversions.Enqueue(body);
            RemoveVisible(body, head: false);
        }

        if (component.PendingDeconversions.Count > 0)
            _pendingCleanupRule = uid;

        component.RevolutionaryMinds.Clear();
        component.RevolutionaryBodies.Clear();
        component.HeadRevolutionaryMinds.Clear();

        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("rev-alert-stage-massacre-end-with-rev-lost"),
            colorOverride: Color.Green,
            usePresetTTS: true);
        _roundEnd.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, component.ShuttleCallTime);
        GameTicker.EndGameRule(uid, gameRule);
    }

    private void ResetRuntimeState(RevolutionaryRuleComponent component)
    {
        component.RevolutionaryMinds.Clear();
        component.RevolutionaryBodies.Clear();
        component.HeadRevolutionaryMinds.Clear();
        component.CommandMinds.Clear();
        component.CommandBodies.Clear();
        component.DeadCommandMinds.Clear();
        component.PendingDeconversions.Clear();
        component.PendingDeconversionSet.Clear();
        component.CommandDeadFraction = 0f;
        component.HadHeadRevolutionaries = false;
        component.HeadCheckPending = false;
        component.ProgressCheckPending = false;
        component.DefeatHandled = false;
        component.SupplyRequested = false;
        component.VoteStarted = false;
        component.Stage = RevolutionaryStage.Initial;
    }

    private void RebuildRuleState(RevolutionaryRuleComponent component)
    {
        component.RevolutionaryMinds.Clear();
        component.RevolutionaryBodies.Clear();
        component.HeadRevolutionaryMinds.Clear();
        component.CommandMinds.Clear();
        component.CommandBodies.Clear();

        var revQuery = EntityQueryEnumerator<RevolutionaryComponent, MindContainerComponent>();
        while (revQuery.MoveNext(out var body, out _, out var container))
        {
            if (!_mobState.IsAlive(body) ||
                !_mind.TryGetMind(body, out var mindId, out _, container))
            {
                continue;
            }

            component.RevolutionaryMinds[mindId] = body;
            component.RevolutionaryBodies[body] = mindId;
            if (HasComp<HeadRevolutionaryComponent>(body))
                component.HeadRevolutionaryMinds.Add(mindId);
        }

        // Be defensive about admin-created heads that omitted the regular marker.
        var headQuery = EntityQueryEnumerator<HeadRevolutionaryComponent, MindContainerComponent>();
        while (headQuery.MoveNext(out var body, out _, out var container))
        {
            if (!_mobState.IsAlive(body) ||
                !_mind.TryGetMind(body, out var mindId, out _, container))
            {
                continue;
            }

            component.RevolutionaryMinds[mindId] = body;
            component.RevolutionaryBodies[body] = mindId;
            component.HeadRevolutionaryMinds.Add(mindId);
        }

        var commandQuery = EntityQueryEnumerator<CommandStaffComponent, MindContainerComponent>();
        while (commandQuery.MoveNext(out var body, out _, out var container))
        {
            if (!_mind.TryGetMind(body, out var mindId, out _, container))
                continue;

            if (component.CommandMinds.TryGetValue(mindId, out var oldBody))
                component.CommandBodies.Remove(oldBody);

            component.CommandMinds[mindId] = body;
            component.CommandBodies[body] = mindId;
            SetCommandDead(component, mindId, IsCommandDead(body));
        }

        if (component.HeadRevolutionaryMinds.Count > 0)
            component.HadHeadRevolutionaries = true;

        UpdateCommandProgress(component);
        component.ProgressCheckPending = true;
    }

    private void RebuildVisibleRoster()
    {
        _visibleRevolutionaries.Clear();
        _visibleHeadRevolutionaries.Clear();

        var revQuery = EntityQueryEnumerator<RevolutionaryComponent>();
        while (revQuery.MoveNext(out var body, out _))
        {
            if (!CanBeVisibleRevolutionary(body))
                continue;

            _visibleRevolutionaries.Add(body);
            if (HasComp<HeadRevolutionaryComponent>(body))
                _visibleHeadRevolutionaries.Add(body);
        }

        var headQuery = EntityQueryEnumerator<HeadRevolutionaryComponent>();
        while (headQuery.MoveNext(out var body, out _))
        {
            if (CanBeVisibleRevolutionary(body))
                _visibleHeadRevolutionaries.Add(body);
        }

        ClearPendingRosterDeltas();
        _rosterViewers.Clear();
        _pendingRosterSnapshots.Clear();
        _pendingRosterClears.Clear();
        _cachedRosterSnapshot = null;
        foreach (var session in _playerManager.Sessions)
        {
            if (CanSeeRoster(session))
            {
                _rosterViewers.Add(session);
                QueueRosterSnapshot(session);
            }
        }
    }

    private bool CanBeVisibleRevolutionary(EntityUid body)
    {
        if (!TryComp<MobStateComponent>(body, out var mobState) ||
            mobState.CurrentState is MobState.Dead or MobState.Invalid)
        {
            return false;
        }

        return _mind.TryGetMind(body, out _, out _) ||
               HasComp<AlwaysRevolutionaryConvertibleComponent>(body);
    }

    private void OnRevolutionaryStartup(EntityUid uid, RevolutionaryComponent component, ComponentStartup args)
    {
        ReconcileRevolutionaryBody(uid);
    }

    private void OnRevolutionaryShutdown(EntityUid uid, RevolutionaryComponent component, ComponentShutdown args)
    {
        RemoveVisible(uid, head: false);
        if (TryGetRule(out var rule) &&
            (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating ||
             !HasComp<HeadRevolutionaryComponent>(uid)))
        {
            RemoveRevolutionaryBody(rule.Comp, uid);
        }
    }

    private void OnHeadRevolutionaryStartup(EntityUid uid, HeadRevolutionaryComponent component, ComponentStartup args)
    {
        ReconcileRevolutionaryBody(uid);
    }

    private void OnHeadRevolutionaryShutdown(EntityUid uid, HeadRevolutionaryComponent component, ComponentShutdown args)
    {
        if (component.HeadRevConvertActionEntity.Valid)
            _actions.RemoveAction(uid, component.HeadRevConvertActionEntity);

        RemoveVisible(uid, head: true);
        if (TryGetRule(out var rule))
        {
            if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating ||
                !HasComp<RevolutionaryComponent>(uid))
            {
                RemoveRevolutionaryBody(rule.Comp, uid);
            }
            else
            {
                RemoveHeadRevolutionaryBody(rule.Comp, uid);
            }

            rule.Comp.HeadCheckPending = true;
        }
    }

    private void OnCommandStartup(EntityUid uid, CommandStaffComponent component, ComponentStartup args)
    {
        if (TryGetRule(out var rule))
            ReconcileCommandBody(rule.Comp, uid);
    }

    private void OnCommandShutdown(EntityUid uid, CommandStaffComponent component, ComponentShutdown args)
    {
        if (!TryGetRule(out var rule))
            return;

        RemoveCommandBody(
            rule.Comp,
            uid,
            preserveAsDead: MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating);
    }

    private void OnRevolutionaryMobStateChanged(
        EntityUid uid,
        RevolutionaryComponent component,
        MobStateChangedEvent args)
    {
        ReconcileRevolutionaryBody(uid);
    }

    private void OnHeadRevolutionaryMobStateChanged(
        EntityUid uid,
        HeadRevolutionaryComponent component,
        MobStateChangedEvent args)
    {
        ReconcileRevolutionaryBody(uid);
        if (TryGetRule(out var rule))
            rule.Comp.HeadCheckPending = true;
    }

    private void OnCommandMobStateChanged(
        EntityUid uid,
        CommandStaffComponent component,
        MobStateChangedEvent args)
    {
        if (!TryGetRule(out var rule))
            return;

        ReconcileCommandBody(rule.Comp, uid);
    }

    private void OnRevolutionaryMindAdded(
        EntityUid uid,
        RevolutionaryComponent component,
        MindAddedMessage args)
    {
        ReconcileRevolutionaryBody(uid, args.Mind.Owner);
    }

    private void OnRevolutionaryMindRemoved(
        EntityUid uid,
        RevolutionaryComponent component,
        MindRemovedMessage args)
    {
        HandleRevolutionaryMindRemoved(uid, args.Mind.Owner, args.TransferEntity);
    }

    private void OnHeadMindAdded(
        EntityUid uid,
        HeadRevolutionaryComponent component,
        MindAddedMessage args)
    {
        ReconcileRevolutionaryBody(uid, args.Mind.Owner);
    }

    private void OnHeadMindRemoved(
        EntityUid uid,
        HeadRevolutionaryComponent component,
        MindRemovedMessage args)
    {
        HandleRevolutionaryMindRemoved(uid, args.Mind.Owner, args.TransferEntity);
    }

    private void OnCommandMindAdded(
        EntityUid uid,
        CommandStaffComponent component,
        MindAddedMessage args)
    {
        if (TryGetRule(out var rule))
            ReconcileCommandBody(rule.Comp, uid, args.Mind.Owner);
    }

    private void OnCommandMindRemoved(
        EntityUid uid,
        CommandStaffComponent component,
        MindRemovedMessage args)
    {
        if (TryGetRule(out var rule))
            HandleCommandMindRemoved(rule.Comp, uid, args.Mind.Owner, args.TransferEntity);
    }

    private void HandleRevolutionaryMindRemoved(
        EntityUid oldBody,
        EntityUid mindId,
        EntityUid? transferEntity)
    {
        RemoveVisible(oldBody, head: true);
        RemoveVisible(oldBody, head: false);

        if (TryGetRule(out var rule))
            RemoveRevolutionaryBody(rule.Comp, oldBody);

        if (transferEntity is { } target && Exists(target))
            ReconcileRevolutionaryBody(target, mindId);
    }

    private void ReconcileRevolutionaryBody(EntityUid body, EntityUid? knownMind = null)
    {
        if (!Exists(body))
            return;

        var revolutionary = HasComp<RevolutionaryComponent>(body);
        var head = HasComp<HeadRevolutionaryComponent>(body);
        var visible = (revolutionary || head) && CanBeVisibleRevolutionary(body);

        if (visible)
        {
            if (revolutionary)
                AddVisible(body, head: false);
            if (head)
                AddVisible(body, head: true);
        }
        else
        {
            RemoveVisible(body, head: true);
            RemoveVisible(body, head: false);
        }

        if (!TryGetRule(out var rule))
            return;

        if (!visible ||
            !(knownMind is { } mindId || _mind.TryGetMind(body, out mindId, out _)))
        {
            RemoveRevolutionaryBody(rule.Comp, body);
        }
        else
        {
            RegisterRevolutionaryMind(rule.Comp, mindId, body, head);
        }
    }

    private void RegisterRevolutionaryMind(
        RevolutionaryRuleComponent component,
        EntityUid mindId,
        EntityUid body,
        bool head)
    {
        if (component.RevolutionaryMinds.TryGetValue(mindId, out var oldBody) && oldBody != body)
            component.RevolutionaryBodies.Remove(oldBody);

        if (component.RevolutionaryBodies.TryGetValue(body, out var oldMind) && oldMind != mindId)
        {
            component.RevolutionaryMinds.Remove(oldMind);
            if (component.HeadRevolutionaryMinds.Remove(oldMind))
                component.HeadCheckPending = true;
        }

        component.RevolutionaryMinds[mindId] = body;
        component.RevolutionaryBodies[body] = mindId;

        if (head)
        {
            component.HeadRevolutionaryMinds.Add(mindId);
            component.HadHeadRevolutionaries = true;
            component.ProgressCheckPending = true;
        }
        else if (component.HeadRevolutionaryMinds.Remove(mindId))
        {
            component.HeadCheckPending = true;
        }
    }

    private void RemoveRevolutionaryBody(RevolutionaryRuleComponent component, EntityUid body)
    {
        if (!component.RevolutionaryBodies.Remove(body, out var mindId))
            return;

        if (component.RevolutionaryMinds.TryGetValue(mindId, out var current) && current == body)
            component.RevolutionaryMinds.Remove(mindId);

        if (component.HeadRevolutionaryMinds.Remove(mindId))
            component.HeadCheckPending = true;
    }

    private static void RemoveHeadRevolutionaryBody(
        RevolutionaryRuleComponent component,
        EntityUid body)
    {
        if (component.RevolutionaryBodies.TryGetValue(body, out var mindId) &&
            component.HeadRevolutionaryMinds.Remove(mindId))
        {
            component.HeadCheckPending = true;
        }
    }

    private void ReconcileCommandBody(
        RevolutionaryRuleComponent component,
        EntityUid body,
        EntityUid? knownMind = null)
    {
        if (!HasComp<CommandStaffComponent>(body))
        {
            RemoveCommandBody(component, body, preserveAsDead: false);
            return;
        }

        if (!(knownMind is { } mindId || _mind.TryGetMind(body, out mindId, out _)))
        {
            if (component.CommandBodies.TryGetValue(body, out var trackedMind))
            {
                SetCommandDead(component, trackedMind, IsCommandDead(body));
                UpdateCommandProgress(component);
            }

            return;
        }

        if (component.CommandMinds.TryGetValue(mindId, out var oldBody) && oldBody != body)
            component.CommandBodies.Remove(oldBody);

        if (component.CommandBodies.TryGetValue(body, out var oldMind) && oldMind != mindId)
        {
            component.CommandMinds.Remove(oldMind);
            component.DeadCommandMinds.Remove(oldMind);
        }

        component.CommandMinds[mindId] = body;
        component.CommandBodies[body] = mindId;
        SetCommandDead(component, mindId, IsCommandDead(body));
        UpdateCommandProgress(component);
    }

    private void HandleCommandMindRemoved(
        RevolutionaryRuleComponent component,
        EntityUid oldBody,
        EntityUid mindId,
        EntityUid? transferEntity)
    {
        if (transferEntity is { } target &&
            Exists(target) &&
            HasComp<CommandStaffComponent>(target))
        {
            ReconcileCommandBody(component, target, mindId);
            return;
        }

        // A command member becoming a ghost or taking another ghost role must remain in
        // the round's command denominator. Their old body continues to provide the death state.
        if (component.CommandBodies.TryGetValue(oldBody, out var trackedMind) &&
            trackedMind == mindId)
        {
            SetCommandDead(component, mindId, IsCommandDead(oldBody));
            UpdateCommandProgress(component);
        }
    }

    private static void RemoveCommandBody(
        RevolutionaryRuleComponent component,
        EntityUid body,
        bool preserveAsDead)
    {
        if (!component.CommandBodies.Remove(body, out var mindId))
            return;

        if (!component.CommandMinds.TryGetValue(mindId, out var current) || current != body)
            return;

        if (preserveAsDead)
        {
            component.CommandMinds[mindId] = EntityUid.Invalid;
            component.DeadCommandMinds.Add(mindId);
        }
        else
        {
            component.CommandMinds.Remove(mindId);
            component.DeadCommandMinds.Remove(mindId);
        }

        UpdateCommandProgress(component);
    }

    private static void SetCommandDead(
        RevolutionaryRuleComponent component,
        EntityUid mindId,
        bool dead)
    {
        if (dead)
            component.DeadCommandMinds.Add(mindId);
        else
            component.DeadCommandMinds.Remove(mindId);
    }

    private static void UpdateCommandProgress(RevolutionaryRuleComponent component)
    {
        var progress = component.CommandMinds.Count == 0
            ? 1f
            : component.DeadCommandMinds.Count / (float) component.CommandMinds.Count;
        if (component.CommandDeadFraction.Equals(progress))
            return;

        component.CommandDeadFraction = progress;
        component.ProgressCheckPending = true;
    }

    private bool IsCommandDead(EntityUid body)
    {
        if (!Exists(body) ||
            !TryComp<MobStateComponent>(body, out var state))
        {
            return true;
        }

        return state.CurrentState is MobState.Dead or MobState.Invalid;
    }

    private void AddVisible(EntityUid body, bool head)
    {
        var wasEligible = TryGetAttachedSession(body, out var attached) &&
                          attached != null &&
                          CanSeeRoster(attached);
        var roster = head ? _visibleHeadRevolutionaries : _visibleRevolutionaries;
        if (!roster.Add(body))
            return;

        var icon = head
            ? Comp<HeadRevolutionaryComponent>(body).StatusIcon
            : Comp<RevolutionaryComponent>(body).StatusIcon;
        QueueRosterAddition(GetNetEntity(body), icon, head);

        if (!wasEligible && attached != null && CanSeeRoster(attached))
            QueueRosterSnapshot(attached);
    }

    private void RemoveVisible(EntityUid body, bool head)
    {
        var roster = head ? _visibleHeadRevolutionaries : _visibleRevolutionaries;
        if (!roster.Contains(body))
            return;

        var wasEligible = TryGetAttachedSession(body, out var attached) &&
                          attached != null &&
                          CanSeeRoster(attached);
        QueueRosterRemoval(GetNetEntity(body), head);
        roster.Remove(body);

        if (wasEligible && attached != null && !CanSeeRoster(attached))
            QueueRosterClear(attached);
    }

    private void QueueRosterAddition(
        NetEntity body,
        ProtoId<FactionIconPrototype> icon,
        bool head)
    {
        var additions = head
            ? _pendingAddedHeadRevolutionaries
            : _pendingAddedRevolutionaries;
        var removals = head
            ? _pendingRemovedHeadRevolutionaries
            : _pendingRemovedRevolutionaries;

        removals.Remove(body);
        additions[body] = icon;

        _cachedRosterSnapshot = null;
    }

    private void QueueRosterRemoval(NetEntity body, bool head)
    {
        var additions = head
            ? _pendingAddedHeadRevolutionaries
            : _pendingAddedRevolutionaries;
        var removals = head
            ? _pendingRemovedHeadRevolutionaries
            : _pendingRemovedRevolutionaries;

        if (!additions.Remove(body))
            removals.Add(body);

        _cachedRosterSnapshot = null;
    }

    private bool CanSeeRoster(ICommonSession session)
    {
        return session.AttachedEntity is { } body &&
               (_visibleRevolutionaries.Contains(body) ||
                _visibleHeadRevolutionaries.Contains(body) ||
                HasComp<ShowAntagIconsComponent>(body));
    }

    private bool TryGetAttachedSession(EntityUid body, out ICommonSession? session)
    {
        if (TryComp<ActorComponent>(body, out var actor))
        {
            session = actor.PlayerSession;
            return true;
        }

        session = null;
        return false;
    }

    private void QueueRosterSnapshot(ICommonSession session)
    {
        _rosterViewers.Add(session);
        _pendingRosterClears.Remove(session);
        _pendingRosterSnapshots.Add(session);
    }

    private void QueueRosterClear(ICommonSession session)
    {
        _rosterViewers.Remove(session);
        _pendingRosterSnapshots.Remove(session);
        _pendingRosterClears.Add(session);
    }

    private void QueueRosterRefresh(ICommonSession session)
    {
        if (CanSeeRoster(session))
            QueueRosterSnapshot(session);
        else
            QueueRosterClear(session);
    }

    private RevolutionaryRosterSyncEvent GetRosterSnapshot()
    {
        if (_cachedRosterSnapshot != null)
            return _cachedRosterSnapshot;

        var revolutionaries = new Dictionary<NetEntity, ProtoId<FactionIconPrototype>>();
        foreach (var uid in _visibleRevolutionaries)
        {
            if (TryComp<RevolutionaryComponent>(uid, out var component))
                revolutionaries[GetNetEntity(uid)] = component.StatusIcon;
        }

        var headRevolutionaries = new Dictionary<NetEntity, ProtoId<FactionIconPrototype>>();
        foreach (var uid in _visibleHeadRevolutionaries)
        {
            if (TryComp<HeadRevolutionaryComponent>(uid, out var component))
                headRevolutionaries[GetNetEntity(uid)] = component.StatusIcon;
        }

        _cachedRosterSnapshot = new RevolutionaryRosterSyncEvent(revolutionaries, headRevolutionaries);
        RosterSnapshotBuildCount++;
        return _cachedRosterSnapshot;
    }

    internal void FlushRosterChanges()
    {
        var hasDelta = _pendingAddedRevolutionaries.Count != 0 ||
                       _pendingRemovedRevolutionaries.Count != 0 ||
                       _pendingAddedHeadRevolutionaries.Count != 0 ||
                       _pendingRemovedHeadRevolutionaries.Count != 0;

        if (hasDelta)
        {
            var filter = Filter.Empty();
            foreach (var session in _rosterViewers)
            {
                if (!_pendingRosterSnapshots.Contains(session) &&
                    !_pendingRosterClears.Contains(session))
                {
                    filter.AddPlayer(session);
                }
            }

            if (filter.Count != 0)
            {
                RaiseNetworkEvent(
                    new RevolutionaryRosterDeltaEvent(
                        new Dictionary<NetEntity, ProtoId<FactionIconPrototype>>(_pendingAddedRevolutionaries),
                        _pendingRemovedRevolutionaries.ToArray(),
                        new Dictionary<NetEntity, ProtoId<FactionIconPrototype>>(_pendingAddedHeadRevolutionaries),
                        _pendingRemovedHeadRevolutionaries.ToArray()),
                    filter,
                    recordReplay: false);
                RosterDeltaBatchCount++;
            }
        }

        if (_pendingRosterSnapshots.Count != 0)
        {
            var filter = Filter.Empty();
            foreach (var session in _pendingRosterSnapshots)
            {
                if (CanSeeRoster(session))
                    filter.AddPlayer(session);
            }

            if (filter.Count != 0)
            {
                RaiseNetworkEvent(GetRosterSnapshot(), filter, recordReplay: false);
                RosterSnapshotBatchCount++;
                RosterSnapshotSendCount += filter.Count;
            }
        }

        if (_pendingRosterClears.Count != 0)
        {
            RaiseNetworkEvent(
                new RevolutionaryRosterClearEvent(),
                Filter.Empty().AddPlayers(_pendingRosterClears),
                recordReplay: false);
        }

        ClearPendingRosterDeltas();
        _pendingRosterSnapshots.Clear();
        _pendingRosterClears.Clear();
    }

    private void ClearPendingRosterDeltas()
    {
        _pendingAddedRevolutionaries.Clear();
        _pendingRemovedRevolutionaries.Clear();
        _pendingAddedHeadRevolutionaries.Clear();
        _pendingRemovedHeadRevolutionaries.Clear();
    }

    internal void ResetRosterDiagnostics()
    {
        RosterDeltaBatchCount = 0;
        RosterSnapshotBuildCount = 0;
        RosterSnapshotBatchCount = 0;
        RosterSnapshotSendCount = 0;
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        QueueRosterRefresh(args.Player);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        _pendingRosterSnapshots.Remove(args.Player);
        _pendingRosterClears.Remove(args.Player);
        _rosterViewers.Remove(args.Player);
    }

    private void OnShowAntagIconsStartup(
        EntityUid uid,
        ShowAntagIconsComponent component,
        ComponentStartup args)
    {
        if (TryGetAttachedSession(uid, out var session) && session != null)
            QueueRosterSnapshot(session);
    }

    private void OnShowAntagIconsShutdown(
        EntityUid uid,
        ShowAntagIconsComponent component,
        ComponentShutdown args)
    {
        if (TryGetAttachedSession(uid, out var session) &&
            session != null &&
            !_visibleRevolutionaries.Contains(uid) &&
            !_visibleHeadRevolutionaries.Contains(uid))
        {
            QueueRosterClear(session);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        RaiseNetworkEvent(
            new RevolutionaryRosterClearEvent(),
            Filter.Empty().AddPlayers(_playerManager.Sessions),
            recordReplay: false);

        _visibleRevolutionaries.Clear();
        _visibleHeadRevolutionaries.Clear();
        _rosterViewers.Clear();
        _cachedRosterSnapshot = null;
        ClearPendingRosterDeltas();
        _pendingRosterSnapshots.Clear();
        _pendingRosterClears.Clear();
        _cachedRule = null;
        _pendingCleanupRule = null;
    }

    private bool TryGetRule(out Entity<RevolutionaryRuleComponent> rule)
    {
        if (_cachedRule is { } cached &&
            TryComp<RevolutionaryRuleComponent>(cached, out var cachedComponent))
        {
            rule = (cached, cachedComponent);
            return true;
        }

        var active = QueryActiveRules();
        if (active.MoveNext(out var uid, out _, out var component, out _))
        {
            _cachedRule = uid;
            rule = (uid, component);
            return true;
        }

        // Integration tests and benchmarks can exercise conversion without starting a full round.
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent>();
        if (query.MoveNext(out uid, out component))
        {
            _cachedRule = uid;
            rule = (uid, component);
            return true;
        }

        rule = default;
        return false;
    }

    private static float GetObjectiveProgress(
        RevolutionaryRuleComponent component,
        float targetFraction)
    {
        return targetFraction <= 0f
            ? 1f
            : Math.Min(component.CommandDeadFraction / targetFraction, 1f);
    }

    private void OnGetBriefing(EntityUid uid, RevolutionaryRoleComponent component, ref GetBriefingEvent args)
    {
        var body = args.Mind.Comp.OwnedEntity;
        args.Append(Loc.GetString(
            HasComp<HeadRevolutionaryComponent>(body)
                ? "head-rev-briefing"
                : "rev-briefing"));
    }

    private void OnHeadMapInit(EntityUid uid, HeadRevolutionaryComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, component.HeadRevConvertAction, component.HeadRevConvertActionEntity);
    }

    private void OnTargetWithConvertWindow(
        EntityUid uid,
        HeadRevolutionaryComponent component,
        ref HeadRevConvertActionEvent args)
    {
        var targetName = MetaData(args.Target).EntityName;
        if (!CanConvert(uid, args.Target, out _, out var mind) ||
            mind?.UserId == null ||
            !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
        {
            _popup.PopupEntity(
                Loc.GetString("head-rev-cant-convert-attempt", ("target", targetName)),
                args.Target,
                uid);
            return;
        }

        _adminLogManager.Add(
            LogType.Mind,
            LogImpact.Medium,
            $"{ToPrettyString(args.Performer)} sent an invitation to {ToPrettyString(args.Target)} to become a Revolutionary");
        _popup.PopupEntity(
            Loc.GetString("head-rev-on-convert-attempt", ("target", targetName)),
            args.Target,
            uid);
        _euiMan.OpenEui(new BecomeRevEui(uid, args.Target, this), client);
    }

    private void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent component, ref AfterFlashedEvent args)
    {
        if (uid != args.User ||
            !args.Melee ||
            !TryGetRule(out var rule) ||
            rule.Comp.Stage != RevolutionaryStage.Massacre)
        {
            return;
        }

        Convert(uid, args.Target);
    }

    /// <summary>
    /// Converts a valid target exactly once.
    /// </summary>
    public bool Convert(EntityUid headRevUid, EntityUid targetUid)
    {
        if (!CanConvert(headRevUid, targetUid, out var mindId, out var mind))
            return false;

        _npcFaction.AddFaction(targetUid, RevolutionaryNpcFaction);
        var revComponent = EnsureComp<RevolutionaryComponent>(targetUid);

        _adminLogManager.Add(
            LogType.Mind,
            LogImpact.Medium,
            $"{ToPrettyString(headRevUid)} converted {ToPrettyString(targetUid)} into a Revolutionary");

        if (_mind.TryGetMind(headRevUid, out var converterMindId, out _) &&
            _role.MindHasRole<RevolutionaryRoleComponent>(converterMindId, out var converterRole))
        {
            converterRole.Value.Comp2.ConvertedCount++;
            Dirty(converterRole.Value.Owner, converterRole.Value.Comp2);
        }

        if (mind != null)
        {
            _role.MindAddRole(mindId, "MindRoleRevolutionary");

            EnsureRevolutionaryObjective(mindId, mind);

            if (mind.UserId != null && _player.TryGetSessionById(mind.UserId, out var session))
            {
                _antag.SendBriefing(
                    session,
                    Loc.GetString("rev-role-greeting"),
                    Color.Red,
                    revComponent.RevStartSound);
            }
        }

        return true;
    }

    private bool CanConvert(
        EntityUid headRevUid,
        EntityUid targetUid,
        out EntityUid mindId,
        out MindComponent? mind)
    {
        mindId = default;
        mind = null;

        if (!Exists(headRevUid) ||
            !HasComp<HeadRevolutionaryComponent>(headRevUid) ||
            !_mobState.IsAlive(headRevUid) ||
            !Exists(targetUid))
        {
            return false;
        }

        var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(targetUid);
        var hasMind = _mind.TryGetMind(targetUid, out mindId, out mind);
        if (!hasMind && !alwaysConvertible)
            return false;

        if (HasComp<RevolutionaryComponent>(targetUid) ||
            HasComp<HeadRevolutionaryComponent>(targetUid) ||
            HasComp<MindShieldComponent>(targetUid) ||
            (!HasComp<HumanoidAppearanceComponent>(targetUid) && !alwaysConvertible) ||
            !_mobState.IsAlive(targetUid) ||
            HasComp<ZombieComponent>(targetUid))
        {
            return false;
        }

        return !hasMind || !_role.MindHasRole<RevolutionaryRoleComponent>(mindId);
    }

    private void EnsureRevolutionaryObjective(EntityUid mindId, MindComponent mind)
    {
        foreach (var objective in mind.Objectives)
        {
            if (Exists(objective) && MetaData(objective).EntityPrototype?.ID == Objective.Id)
                return;
        }

        _mind.TryAddObjective(mindId, mind, Objective);
    }

    /// <summary>
    /// Removes all revolutionary state from the current body and mind. Safe to call repeatedly.
    /// </summary>
    public bool Deconvert(
        EntityUid targetUid,
        bool stun = true,
        bool showPopup = true,
        bool showEui = false,
        string reason = "deconverted")
    {
        if (!Exists(targetUid) || HasComp<HeadRevolutionaryComponent>(targetUid))
            return false;

        var hadComponent = HasComp<RevolutionaryComponent>(targetUid);
        var hasMind = _mind.TryGetMind(targetUid, out var mindId, out var mind);
        var hadRole = hasMind && _role.MindHasRole<RevolutionaryRoleComponent>(mindId);
        if (!hadComponent && !hadRole)
            return false;

        _npcFaction.RemoveFaction(targetUid, RevolutionaryNpcFaction);
        if (hadComponent)
        {
            RemComp<RevolutionaryComponent>(targetUid);
        }
        else
        {
            RemoveVisible(targetUid, head: false);
            if (TryGetRule(out var rule))
                RemoveRevolutionaryBody(rule.Comp, targetUid);
        }

        if (hasMind && mind != null)
        {
            RemoveRevolutionaryObjectives(mindId, mind);
            _role.MindRemoveRole<RevolutionaryRoleComponent>(mindId);
        }

        if (stun)
            _stun.TryUpdateParalyzeDuration(targetUid, TimeSpan.FromSeconds(4));

        if (showPopup)
        {
            _popup.PopupEntity(
                Loc.GetString(
                    "rev-break-control",
                    ("name", Identity.Entity(targetUid, EntityManager))),
                targetUid);
        }

        if (showEui &&
            hasMind &&
            mind != null &&
            mind.UserId != null &&
            _player.TryGetSessionById(mind.UserId, out var session))
        {
            _euiMan.OpenEui(new DeconvertedEui(), session);
        }

        _adminLogManager.Add(
            LogType.Mind,
            LogImpact.Medium,
            $"{ToPrettyString(targetUid)} was deconverted: {reason}.");
        return true;
    }

    private void RemoveRevolutionaryObjectives(EntityUid mindId, MindComponent mind)
    {
        for (var index = mind.Objectives.Count - 1; index >= 0; index--)
        {
            var objective = mind.Objectives[index];
            if (Exists(objective) && MetaData(objective).EntityPrototype?.ID == Objective.Id)
                _mind.TryRemoveObjective(mindId, mind, index);
        }
    }
}

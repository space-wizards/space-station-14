using Content.Server.DeadSpace.Traitor;
using Content.Server.DeadSpace.Traitor.Objectives;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Shuttles.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Components;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Shuttles.Systems;

// TODO full game saves
// Move state data into the emergency shuttle component
public sealed partial class EmergencyShuttleSystem
{
    /*
     * Handles the emergency shuttle's console and early launching.
     */

    /// <summary>
    /// Has the emergency shuttle arrived?
    /// </summary>
    public bool EmergencyShuttleArrived { get; private set; }

    public bool EarlyLaunchAuthorized { get; private set; }

    /// <summary>
    /// How much time remaining until the shuttle consoles for emergency shuttles are unlocked?
    /// </summary>
    private float _consoleAccumulator = float.MinValue;

    /// <summary>
    /// How long after the transit is over to end the round.
    /// </summary>
    private readonly TimeSpan _bufferTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <see cref="CCVars.EmergencyShuttleMinTransitTime"/>
    /// </summary>
    public float MinimumTransitTime { get; private set; }

    /// <summary>
    /// <see cref="CCVars.EmergencyShuttleMaxTransitTime"/>
    /// </summary>
    public float MaximumTransitTime { get; private set; }

    /// <summary>
    /// How long it will take for the emergency shuttle to arrive at CentComm.
    /// </summary>
    public float TransitTime;

    /// <summary>
    /// <see cref="CCVars.EmergencyShuttleAuthorizeTime"/>
    /// </summary>
    private float _authorizeTime;

    // private CancellationTokenSource? _roundEndCancelToken; // DS14

    // DS14-start
    private const string TraitorUltraRaiderOutpostRule = "SyndicateRaid";
    private static readonly TimeSpan TraitorUltraHijackDelay = TimeSpan.FromMinutes(1);
    private const float TraitorUltraEmergencyDockTime = 5 * 60;
    private const float TraitorUltraEmergencyOffTargetDockTime = 7 * 60;
    private TimeSpan? _traitorUltraHijackCompletionTime;
    private EntityUid? _traitorUltraHijackerMind;
    private string _traitorUltraHijackerName = string.Empty;
    private EntityUid? _traitorUltraHijackShuttle;
    private bool _traitorUltraHijackCompleted;
    private bool _traitorUltraHijackArriving;
    // DS14-end

    // DS14-start
    private bool IsTraitorUltraRuleAdded()
    {
        var query = EntityQueryEnumerator<TraitorUltraRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (_ticker.IsGameRuleAdded(uid, gameRule))
                return true;
        }

        return false;
    }

    private float GetTraitorUltraEmergencyDockTime(ShuttleDockResultType resultType)
    {
        return resultType is ShuttleDockResultType.OtherDock or ShuttleDockResultType.NoDock
            ? TraitorUltraEmergencyOffTargetDockTime
            : TraitorUltraEmergencyDockTime;
    }
    // DS14-end

    private static readonly ProtoId<AccessLevelPrototype> EmergencyRepealAllAccess = "EmergencyShuttleRepealAll";
    private static readonly Color DangerColor = Color.Red;

    /// <summary>
    /// Have the emergency shuttles been authorised to launch at CentCom?
    /// </summary>
    private bool _launchedShuttles;

    /// <summary>
    /// Have the emergency shuttles left for CentCom?
    /// </summary>
    public bool ShuttlesLeft;

    /// <summary>
    /// Have we announced the launch?
    /// </summary>
    private bool _announced;

    private void InitializeEmergencyConsole()
    {
        Subs.CVar(ConfigManager, CCVars.EmergencyShuttleMinTransitTime, SetMinTransitTime, true);
        Subs.CVar(ConfigManager, CCVars.EmergencyShuttleMaxTransitTime, SetMaxTransitTime, true);
        Subs.CVar(ConfigManager, CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime, true);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ComponentStartup>(OnEmergencyStartup);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
        // DS14-start
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, BoundUIOpenedEvent>(OnEmergencyConsoleOpened);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleHijackStartMessage>(OnEmergencyHijackStart);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleHijackCancelMessage>(OnEmergencyHijackCancel);
        // DS14-end

    }

    private void SetAuthorizeTime(float obj)
    {
        _authorizeTime = obj;
    }

    private void SetMinTransitTime(float obj)
    {
        MinimumTransitTime = obj;
        MaximumTransitTime = Math.Max(MaximumTransitTime, MinimumTransitTime);
    }

    private void SetMaxTransitTime(float obj)
    {
        MaximumTransitTime = Math.Max(MinimumTransitTime, obj);
    }

    private void OnEmergencyStartup(EntityUid uid, EmergencyShuttleConsoleComponent component, ComponentStartup args)
    {
        UpdateConsoleState(uid, component);
    }

    private void UpdateEmergencyConsole(float frameTime)
    {
        UpdateTraitorUltraHijack(); // DS14

        // Add some buffer time so eshuttle always first.
        var minTime = -(TransitTime - (_shuttle.DefaultStartupTime + _shuttle.DefaultTravelTime + 1f));

        // TODO: I know this is shit but I already just cleaned up a billion things.

        // This is very cursed spaghetti code. I don't even know what the fuck this is doing or why it exists.
        // But I think it needs to be less than or equal to zero or the shuttle might never leave???
        // TODO Shuttle AAAAAAAAAAAAAAAAAAAAAAAAA
        // Clean this up, just have a single timer with some state system.
        // I.e., dont infer state from the current interval that the accumulator is in???
        minTime = Math.Min(0, minTime); // ????

        if (_consoleAccumulator < minTime)
        {
            return;
        }

        // DS14-start
        if (_traitorUltraHijackCompletionTime != null)
            return;
        // DS14-end

        _consoleAccumulator -= frameTime;

        // No early launch but we're under the timer.
        if (!_launchedShuttles && _consoleAccumulator <= _authorizeTime)
        {
            if (!EarlyLaunchAuthorized)
                AnnounceLaunch();
        }

        // Imminent departure
        if (!_launchedShuttles && _consoleAccumulator <= _shuttle.DefaultStartupTime)
        {
            _launchedShuttles = true;

            var dataQuery = AllEntityQuery<StationEmergencyShuttleComponent>();

            while (dataQuery.MoveNext(out var stationUid, out var comp))
            {
                if (!TryComp<ShuttleComponent>(comp.EmergencyShuttle, out var shuttle) ||
                    !TryComp<StationCentcommComponent>(stationUid, out var centcomm))
                {
                    continue;
                }

                if (!Deleted(centcomm.Entity))
                {
                    _shuttle.FTLToDock(comp.EmergencyShuttle.Value, shuttle,
                        centcomm.Entity.Value, _consoleAccumulator, TransitTime);
                    continue;
                }

                if (!Deleted(centcomm.MapEntity))
                {
                    // TODO: Need to get non-overlapping positions.
                    _shuttle.FTLToCoordinates(comp.EmergencyShuttle.Value, shuttle,
                        new EntityCoordinates(centcomm.MapEntity.Value,
                            _random.NextVector2(1000f)), _consoleAccumulator, TransitTime);
                }
            }

            var podQuery = AllEntityQuery<EscapePodComponent>();

            // Stagger launches coz funny
            while (podQuery.MoveNext(out _, out var pod))
            {
                pod.LaunchTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(0.05f, 0.75f));
            }
        }

        var podLaunchQuery = EntityQueryEnumerator<EscapePodComponent, ShuttleComponent>();

        while (podLaunchQuery.MoveNext(out var uid, out var pod, out var shuttle))
        {
            var stationUid = _station.GetOwningStation(uid);

            if (!TryComp<StationCentcommComponent>(stationUid, out var centcomm) ||
                Deleted(centcomm.Entity) ||
                pod.LaunchTime == null ||
                pod.LaunchTime > _timing.CurTime)
            {
                continue;
            }

            // Don't dock them. If you do end up doing this then stagger launch.
            _shuttle.FTLToDock(uid, shuttle, centcomm.Entity.Value, hyperspaceTime: TransitTime);
            RemCompDeferred<EscapePodComponent>(uid);
        }

        // Departed
        if (!ShuttlesLeft && _consoleAccumulator <= 0f)
        {
            ShuttlesLeft = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-left", ("transitTime", $"{TransitTime:0}")));

            _roundEnd.StartRoundEndTimer(TimeSpan.FromMilliseconds((int)(TransitTime * 1000) + _bufferTime.Milliseconds)); // DS14
        }

        // All the others.
        if (_consoleAccumulator < minTime)
        {
            var query = AllEntityQuery<StationCentcommComponent, TransformComponent>();

            // Guarantees that emergency shuttle arrives first before anyone else can FTL.
            while (query.MoveNext(out var comp, out var centcommXform))
            {
                if (Deleted(comp.Entity))
                    continue;

                if (_shuttle.TryAddFTLDestination(centcommXform.MapID, true, out var ftlComp))
                {
                    _shuttle.SetFTLWhitelist((centcommXform.MapUid!.Value, ftlComp), null);
                }
            }
        }
    }

    private void OnEmergencyRepealAll(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealAllMessage args)
    {
        var player = args.Actor;

        // DS14-start
        if (!EmergencyEarlyLaunchAllowed)
        {
            Popup.PopupCursor(Loc.GetString("emergency-shuttle-console-no-early-launches"), player, PopupType.Medium);
            return;
        }
        // DS14-end

        if (!_reader.FindAccessTags(player).Contains(EmergencyRepealAllAccess))
        {
            Popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), player, PopupType.Medium);
            return;
        }

        if (component.AuthorizedEntities.Count == 0)
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL ALL by {args.Actor:user}");
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-console-auth-revoked", ("remaining", component.AuthorizationsRequired)));
        component.AuthorizedEntities.Clear();
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyRepeal(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealMessage args)
    {
        var player = args.Actor;

        // DS14-start
        if (!EmergencyEarlyLaunchAllowed)
        {
            Popup.PopupCursor(Loc.GetString("emergency-shuttle-console-no-early-launches"), player, PopupType.Medium);
            return;
        }
        // DS14-end

        if (!_idSystem.TryFindIdCard(player, out var idCard) || !_reader.IsAllowed(idCard, uid))
        {
            Popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), player, PopupType.Medium);
            return;
        }

        if (!component.AuthorizedEntities.Remove(idCard.Owner))
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL by {args.Actor:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-console-auth-revoked", ("remaining", remaining)));
        CheckForLaunch(component);
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Actor;

        // DS14-start
        if (!EmergencyEarlyLaunchAllowed)
        {
            Popup.PopupCursor(Loc.GetString("emergency-shuttle-console-no-early-launches"), player, PopupType.Medium);
            return;
        }
        // DS14-end

        if (!_idSystem.TryFindIdCard(player, out var idCard) || !_reader.IsAllowed(idCard, uid))
        {
            Popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), args.Actor, PopupType.Medium);
            return;
        }

        var idCardUid = idCard.Owner;

        if (component.AuthorizedEntities.ContainsKey(idCardUid))
            return;

        component.AuthorizedEntities[idCardUid] = MetaData(idCard).EntityName;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch AUTH by {args.Actor:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;

        if (remaining > 0)
            _chatSystem.DispatchGlobalAnnouncement(
                Loc.GetString("emergency-shuttle-console-auth-left", ("remaining", remaining)),
                playSound: false, colorOverride: DangerColor);

        if (!CheckForLaunch(component))
            _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), recordReplay: true);

        UpdateAllEmergencyConsoles();
    }

    // DS14-start
    private void OnEmergencyConsoleOpened(EntityUid uid, EmergencyShuttleConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(EmergencyConsoleUiKey.Key))
            return;

        SendHijackAvailability(uid, args.Actor);
    }

    private void OnEmergencyHijackStart(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleHijackStartMessage args)
    {
        var player = args.Actor;

        if (_traitorUltraHijackCompleted)
        {
            Popup.PopupEntity(Loc.GetString("emergency-shuttle-console-hijack-already-complete"), uid, player, PopupType.MediumCaution);
            SendHijackAvailability(uid, player);
            return;
        }

        if (_traitorUltraHijackCompletionTime != null)
        {
            Popup.PopupEntity(Loc.GetString("emergency-shuttle-console-hijack-already-started"), uid, player, PopupType.MediumCaution);
            SendHijackAvailability(uid, player);
            return;
        }

        if (!TryGetTraitorUltraHijackMind(player, out var mindId))
        {
            Popup.PopupEntity(Loc.GetString("emergency-shuttle-console-hijack-denied"), uid, player, PopupType.MediumCaution);
            SendHijackAvailability(uid, player);
            return;
        }

        _traitorUltraHijackerMind = mindId;
        _traitorUltraHijackerName = MetaData(player).EntityName;
        _traitorUltraHijackCompletionTime = _timing.CurTime + TraitorUltraHijackDelay;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Traitor Ultra shuttle hijack started by {player:user}");
        DispatchTraitorUltraHijackAnnouncement("emergency-shuttle-console-hijack-started", DangerColor);

        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyHijackCancel(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleHijackCancelMessage args)
    {
        if (_traitorUltraHijackCompletionTime == null || _traitorUltraHijackCompleted)
            return;

        if (!CanCancelTraitorUltraHijack(uid, args.Actor))
        {
            Popup.PopupEntity(Loc.GetString("emergency-shuttle-console-hijack-denied"), uid, args.Actor, PopupType.MediumCaution);
            SendHijackAvailability(uid, args.Actor);
            return;
        }

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Traitor Ultra shuttle hijack cancelled by {args.Actor:user}");
        _traitorUltraHijackCompletionTime = null;
        _traitorUltraHijackerMind = null;
        _traitorUltraHijackerName = string.Empty;

        DispatchTraitorUltraHijackAnnouncement("emergency-shuttle-console-hijack-cancelled", Color.LightSkyBlue);

        UpdateAllEmergencyConsoles();
    }

    private void UpdateTraitorUltraHijack()
    {
        if (_traitorUltraHijackCompleted ||
            _traitorUltraHijackCompletionTime == null ||
            _timing.CurTime < _traitorUltraHijackCompletionTime.Value)
        {
            return;
        }

        _traitorUltraHijackCompleted = true;
        _traitorUltraHijackCompletionTime = null;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Traitor Ultra shuttle hijack completed");
        DispatchTraitorUltraHijackAnnouncement("emergency-shuttle-console-hijack-completed", DangerColor);
        StartTraitorUltraHijackJump();

        UpdateAllEmergencyConsoles();
    }

    private void DispatchTraitorUltraHijackAnnouncement(string messageId, Color color)
    {
        var message = Loc.GetString(messageId);

        _chatSystem.DispatchGlobalAnnouncement(
            message,
            Loc.GetString("emergency-shuttle-console-hijack-announcer"),
            playSound: true,
            announcementSound: new SoundPathSpecifier("/Audio/Effects/alert.ogg"),
            colorOverride: color,
            originalMessage: message,
            voice: "Rita");
    }

    private void StartTraitorUltraHijackJump()
    {
        var shuttleUid = GetShuttle();
        if (shuttleUid == null || !TryComp<ShuttleComponent>(shuttleUid.Value, out var shuttle))
        {
            _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Traitor Ultra hijack completed but no emergency shuttle was available; ending round without outpost jump");
            _roundEnd.EndRound();
            return;
        }

        var outpost = GetOrCreateTraitorUltraRaiderOutpost();
        if (outpost == null || !TryComp(outpost.Value, out TransformComponent? outpostXform) || outpostXform.MapUid == null)
        {
            _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Traitor Ultra hijack completed but the raider outpost could not be loaded; ending round without outpost jump");
            _roundEnd.EndRound();
            return;
        }

        DelayEmergencyRoundEnd();
        _traitorUltraHijackShuttle = shuttleUid;
        _traitorUltraHijackArriving = true;
        _launchedShuttles = true;
        ShuttlesLeft = true;
        _consoleAccumulator = float.MinValue;

        var target = new EntityCoordinates(outpostXform.MapUid.Value, _transformSystem.GetWorldPosition(outpostXform));
        _shuttle.FTLToCoordinates(
            shuttleUid.Value,
            shuttle,
            target,
            Angle.Zero,
            startupTime: _shuttle.DefaultStartupTime,
            hyperspaceTime: TransitTime,
            useProximity: true,
            proximityMinOffset: 32f,
            proximityMaxOffset: 96f);

        if (HasComp<FTLComponent>(shuttleUid.Value))
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Traitor Ultra hijack completed but emergency shuttle FTL failed; ending round without outpost arrival");
        _traitorUltraHijackArriving = false;
        _traitorUltraHijackShuttle = null;
        _roundEnd.EndRound();
    }

    private EntityUid? GetOrCreateTraitorUltraRaiderOutpost()
    {
        if (TryFindTraitorUltraRaiderOutpost(out var outpost))
            return outpost;

        if (!_ticker.StartGameRule(TraitorUltraRaiderOutpostRule, out var ruleEntity))
            return null;

        if (!TryComp<RuleGridsComponent>(ruleEntity, out var grids))
            return null;

        return PickTraitorUltraRaiderOutpostGrid(grids);
    }

    private bool TryFindTraitorUltraRaiderOutpost(out EntityUid outpost)
    {
        var query = AllEntityQuery<RuleGridsComponent>();
        while (query.MoveNext(out var uid, out var grids))
        {
            if (MetaData(uid).EntityPrototype?.ID != TraitorUltraRaiderOutpostRule)
                continue;

            if (PickTraitorUltraRaiderOutpostGrid(grids) is not { } found)
                continue;

            outpost = found;
            return true;
        }

        outpost = default;
        return false;
    }

    private EntityUid? PickTraitorUltraRaiderOutpostGrid(RuleGridsComponent grids)
    {
        return PickLargestGrid(grids.MapGrids, skipNukeOpsShuttles: true) ??
               PickLargestGrid(grids.MapGrids, skipNukeOpsShuttles: false);
    }

    private EntityUid? PickLargestGrid(IEnumerable<EntityUid> grids, bool skipNukeOpsShuttles)
    {
        EntityUid? best = null;
        var bestArea = 0f;

        foreach (var gridUid in grids)
        {
            if (Deleted(gridUid) ||
                skipNukeOpsShuttles && HasComp<NukeOpsShuttleComponent>(gridUid) ||
                !TryComp<MapGridComponent>(gridUid, out var grid))
            {
                continue;
            }

            var area = grid.LocalAABB.Width * grid.LocalAABB.Height;
            if (best != null && area <= bestArea)
                continue;

            best = gridUid;
            bestArea = area;
        }

        return best;
    }
    // DS14-end

    private void CleanupEmergencyConsole()
    {
        // Realistically most of this shit needs moving to a station component so each station has their own emergency shuttle
        // and timer and all that jazz so I don't really care about debugging if it works on cleanup vs start.
        _announced = false;
        ShuttlesLeft = false;
        _launchedShuttles = false;
        _consoleAccumulator = float.MinValue;
        // DS14-start
        _traitorUltraHijackCompletionTime = null;
        _traitorUltraHijackerMind = null;
        _traitorUltraHijackerName = string.Empty;
        _traitorUltraHijackShuttle = null;
        _traitorUltraHijackCompleted = false;
        _traitorUltraHijackArriving = false;
        // DS14-end
        EarlyLaunchAuthorized = false;
        EmergencyShuttleArrived = false;
        TransitTime = MinimumTransitTime + (MaximumTransitTime - MinimumTransitTime) * _random.NextFloat();
        // Round to nearest 10
        TransitTime = MathF.Round(TransitTime / 10f) * 10f;
    }

    private void UpdateAllEmergencyConsoles()
    {
        var query = AllEntityQuery<EmergencyShuttleConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateConsoleState(uid, comp);
            SendHijackAvailabilityToOpenActors(uid); // DS14
        }
    }

    private void UpdateConsoleState(EntityUid uid, EmergencyShuttleConsoleComponent component)
    {
        var auths = new List<string>();

        foreach (var auth in component.AuthorizedEntities.Values)
        {
            auths.Add(auth);
        }

        if (_uiSystem.HasUi(uid, EmergencyConsoleUiKey.Key))
            _uiSystem.SetUiState(
                uid,
                EmergencyConsoleUiKey.Key,
                new EmergencyConsoleBoundUserInterfaceState()
                {
                    EarlyLaunchTime = EarlyLaunchAuthorized ? _timing.CurTime + TimeSpan.FromSeconds(_consoleAccumulator) : null,
                    TimeToLaunch = _consoleAccumulator >= 0f ? TimeSpan.FromSeconds(_consoleAccumulator) : null, // DS14
                    Authorizations = auths,
                    AuthorizationsRequired = component.AuthorizationsRequired,
                    // DS14-start
                    EarlyLaunchAllowed = EmergencyEarlyLaunchAllowed,
                    HijackCompletionTime = _traitorUltraHijackCompleted ? null : _traitorUltraHijackCompletionTime,
                    HijackCompleted = _traitorUltraHijackCompleted,
                    HijackerName = _traitorUltraHijackerName,
                    // DS14-end
                }
            );
    }

    // DS14-start
    public bool IsTraitorUltraHijackCompleted(EntityUid mindId)
    {
        return _traitorUltraHijackCompleted && _traitorUltraHijackerMind == mindId;
    }

    private void SendHijackAvailabilityToOpenActors(EntityUid uid)
    {
        foreach (var actor in _uiSystem.GetActors(uid, EmergencyConsoleUiKey.Key))
        {
            SendHijackAvailability(uid, actor);
        }
    }

    private void SendHijackAvailability(EntityUid uid, EntityUid actor)
    {
        _uiSystem.ServerSendUiMessage(
            uid,
            EmergencyConsoleUiKey.Key,
            new EmergencyShuttleHijackAvailabilityMessage(
                CanStartTraitorUltraHijack(actor),
                CanCancelTraitorUltraHijack(uid, actor)),
            actor);
    }

    private bool CanStartTraitorUltraHijack(EntityUid actor)
    {
        return !_traitorUltraHijackCompleted &&
               _traitorUltraHijackCompletionTime == null &&
               TryGetTraitorUltraHijackMind(actor, out _);
    }

    private bool CanCancelTraitorUltraHijack(EntityUid console, EntityUid actor)
    {
        if (_traitorUltraHijackCompleted || _traitorUltraHijackCompletionTime == null)
            return false;

        if (_reader.IsAllowed(actor, console))
            return true;

        return TryGetTraitorUltraHijackMind(actor, out var mindId) &&
               _traitorUltraHijackerMind == mindId;
    }

    private bool TryGetTraitorUltraHijackMind(EntityUid actor, out EntityUid mindId)
    {
        mindId = default;

        if (!TryComp<MindContainerComponent>(actor, out var mindContainer) ||
            mindContainer.Mind is not { } foundMind ||
            !TryComp<MindComponent>(foundMind, out var mind))
        {
            return false;
        }

        mindId = foundMind;
        if (!_roleSystem.MindHasRole<TraitorRoleComponent>(mindId))
            return false;

        foreach (var objective in mind.Objectives)
        {
            if (!TerminatingOrDeleted(objective) &&
                HasComp<TraitorUltraHijackShuttleConditionComponent>(objective))
            {
                return true;
            }
        }

        return false;
    }
    // DS14-end

    private bool CheckForLaunch(EmergencyShuttleConsoleComponent component)
    {
        if (component.AuthorizedEntities.Count < component.AuthorizationsRequired || EarlyLaunchAuthorized)
            return false;

        EarlyLaunch();
        return true;
    }

    /// <summary>
    /// Attempts to early launch the emergency shuttle if not already done.
    /// </summary>
    public bool EarlyLaunch()
    {
        if (EarlyLaunchAuthorized || !EmergencyShuttleArrived || _consoleAccumulator <= _authorizeTime) return false;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle launch authorized");
        _consoleAccumulator = _authorizeTime;
        EarlyLaunchAuthorized = true;
        RaiseLocalEvent(new EmergencyShuttleAuthorizedEvent());
        AnnounceLaunch();
        UpdateAllEmergencyConsoles();

        var time = TimeSpan.FromSeconds(_authorizeTime);
        var shuttle = GetShuttle();
        if (shuttle != null && TryComp<DeviceNetworkComponent>(shuttle, out var net))
        {
            var payload = new NetworkPayload
            {
                [ShuttleTimerMasks.ShuttleMap] = shuttle,
                [ShuttleTimerMasks.SourceMap] = _roundEnd.GetStation(),
                [ShuttleTimerMasks.DestMap] = _roundEnd.GetCentcomm(),
                [ShuttleTimerMasks.ShuttleTime] = time,
                [ShuttleTimerMasks.SourceTime] = time,
                [ShuttleTimerMasks.DestTime] = time + TimeSpan.FromSeconds(TransitTime),
                [ShuttleTimerMasks.Docked] = true
            };
            _deviceNetworkSystem.QueuePacket(shuttle.Value, null, payload, net.TransmitFrequency);
        }

        return true;
    }

    private void AnnounceLaunch()
    {
        if (_announced) return;

        _announced = true;
        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("emergency-shuttle-launch-time", ("consoleAccumulator", $"{_consoleAccumulator:0}")),
            playSound: false,
            colorOverride: DangerColor);

        _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), recordReplay: true);
    }

    public bool DelayEmergencyRoundEnd()
    {
        return _roundEnd.PauseRoundTransitionTimer(); // DS14
    }
}

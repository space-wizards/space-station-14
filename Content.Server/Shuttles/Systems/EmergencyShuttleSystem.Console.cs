using System.Threading;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

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

    private CancellationTokenSource? _roundEndCancelToken;

    [ValidatePrototypeId<AccessLevelPrototype>]
    private const string EmergencyRepealAllAccess = "EmergencyShuttleRepealAll";
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
        Subs.CVar(_configManager, CCVars.EmergencyShuttleMinTransitTime, SetMinTransitTime, true);
        Subs.CVar(_configManager, CCVars.EmergencyShuttleMaxTransitTime, SetMaxTransitTime, true);
        Subs.CVar(_configManager, CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime, true);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ComponentStartup>(OnEmergencyStartup);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ActivatableUIOpenAttemptEvent>(OnEmergencyOpenAttempt);
    }

    private void OnEmergencyOpenAttempt(EntityUid uid, EmergencyShuttleConsoleComponent component, ActivatableUIOpenAttemptEvent args)
    {
        // I'm hoping ActivatableUI checks it's open before allowing these messages.
        if (!_configManager.GetCVar(CCVars.EmergencyEarlyLaunchAllowed))
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString("emergency-shuttle-console-no-early-launches"), uid, args.User);
        }
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

            Timer.Spawn((int)(TransitTime * 1000) + _bufferTime.Milliseconds, () => _roundEnd.EndRound(), _roundEndCancelToken?.Token ?? default);
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

        if (!_reader.FindAccessTags(player).Contains(EmergencyRepealAllAccess))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), player, PopupType.Medium);
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

        if (!_idSystem.TryFindIdCard(player, out var idCard) || !_reader.IsAllowed(idCard, uid))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), player, PopupType.Medium);
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Remove(MetaData(idCard).EntityName))
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

        if (!_idSystem.TryFindIdCard(player, out var idCard) || !_reader.IsAllowed(idCard, uid))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), args.Actor, PopupType.Medium);
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(MetaData(idCard).EntityName))
            return;

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

    private void CleanupEmergencyConsole()
    {
        // Realistically most of this shit needs moving to a station component so each station has their own emergency shuttle
        // and timer and all that jazz so I don't really care about debugging if it works on cleanup vs start.
        _announced = false;
        ShuttlesLeft = false;
        _launchedShuttles = false;
        _consoleAccumulator = float.MinValue;
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
        }
    }

    private void UpdateConsoleState(EntityUid uid, EmergencyShuttleConsoleComponent component)
    {
        var auths = new List<string>();

        foreach (var auth in component.AuthorizedEntities)
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
                    Authorizations = auths,
                    AuthorizationsRequired = component.AuthorizationsRequired,
                }
            );
    }

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

        _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Emergency shuttle launch authorized");
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
        if (_roundEndCancelToken == null)
            return false;

        _roundEndCancelToken?.Cancel();
        _roundEndCancelToken = null;
        return true;
    }
}

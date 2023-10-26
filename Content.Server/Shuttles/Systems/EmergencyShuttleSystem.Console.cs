using System.Threading;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.UserInterface;
using Content.Shared.Access;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Shuttles.Systems;

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
    public float TransitTime { get; private set; }

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
        _configManager.OnValueChanged(CCVars.EmergencyShuttleMinTransitTime, SetMinTransitTime, true);
        _configManager.OnValueChanged(CCVars.EmergencyShuttleMaxTransitTime, SetMaxTransitTime, true);
        _configManager.OnValueChanged(CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime, true);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ComponentStartup>(OnEmergencyStartup);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ActivatableUIOpenAttemptEvent>(OnEmergencyOpenAttempt);

        SubscribeLocalEvent<EscapePodComponent, EntityUnpausedEvent>(OnEscapeUnpaused);
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

    private void ShutdownEmergencyConsole()
    {
        _configManager.UnsubValueChanged(CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime);
        _configManager.UnsubValueChanged(CCVars.EmergencyShuttleMinTransitTime, SetMinTransitTime);
        _configManager.UnsubValueChanged(CCVars.EmergencyShuttleMaxTransitTime, SetMaxTransitTime);
    }

    private void OnEmergencyStartup(EntityUid uid, EmergencyShuttleConsoleComponent component, ComponentStartup args)
    {
        UpdateConsoleState(uid, component);
    }

    private void UpdateEmergencyConsole(float frameTime)
    {
        // Add some buffer time so eshuttle always first.
        var minTime = -(TransitTime - (ShuttleSystem.DefaultStartupTime + ShuttleSystem.DefaultTravelTime + 1f));

        // TODO: I know this is shit but I already just cleaned up a billion things.
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
        if (!_launchedShuttles && _consoleAccumulator <= ShuttleSystem.DefaultStartupTime)
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

                if (Deleted(centcomm.Entity))
                {
                    // TODO: Need to get non-overlapping positions.
                    _shuttle.FTLTravel(comp.EmergencyShuttle.Value, shuttle,
                        new EntityCoordinates(
                            _mapManager.GetMapEntityId(centcomm.MapId),
                            _random.NextVector2(1000f)), _consoleAccumulator, TransitTime);
                }
                else
                {
                    _shuttle.FTLTravel(comp.EmergencyShuttle.Value, shuttle,
                        centcomm.Entity, _consoleAccumulator, TransitTime, true);
                }
            }

            var podQuery = AllEntityQuery<EscapePodComponent>();
            var podLaunchOffset = 0.5f;

            // Stagger launches coz funny
            while (podQuery.MoveNext(out _, out var pod))
            {
                pod.LaunchTime = _timing.CurTime + TimeSpan.FromSeconds(podLaunchOffset);
                podLaunchOffset += _random.NextFloat(0.5f, 2.5f);
            }
        }

        var podLaunchQuery = EntityQueryEnumerator<EscapePodComponent, ShuttleComponent>();

        while (podLaunchQuery.MoveNext(out var uid, out var pod, out var shuttle))
        {
            var stationUid = _station.GetOwningStation(uid);

            if (!TryComp<StationCentcommComponent>(stationUid, out var centcomm) ||
                Deleted(centcomm.Entity) || pod.LaunchTime == null || pod.LaunchTime < _timing.CurTime)
            {
                continue;
            }

            // Don't dock them. If you do end up doing this then stagger launch.
            _shuttle.FTLTravel(uid, shuttle, centcomm.Entity, hyperspaceTime: TransitTime);
            RemCompDeferred<EscapePodComponent>(uid);
        }

        // Departed
        if (!ShuttlesLeft && _consoleAccumulator <= 0f)
        {
            ShuttlesLeft = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-left", ("transitTime", $"{TransitTime:0}")));

            Timer.Spawn((int) (TransitTime * 1000) + _bufferTime.Milliseconds, () => _roundEnd.EndRound(), _roundEndCancelToken?.Token ?? default);
        }

        // All the others.
        if (_consoleAccumulator < minTime)
        {
            var query = AllEntityQuery<StationCentcommComponent>();

            // Guarantees that emergency shuttle arrives first before anyone else can FTL.
            while (query.MoveNext(out var comp))
            {
                if (Deleted(comp.Entity))
                    continue;

                _shuttle.AddFTLDestination(comp.Entity, true);
            }
        }
    }

    private void OnEmergencyRepealAll(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealAllMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null) return;

        if (!_reader.FindAccessTags(player.Value).Contains(EmergencyRepealAllAccess))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), player.Value, PopupType.Medium);
            return;
        }

        if (component.AuthorizedEntities.Count == 0)
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL ALL by {args.Session:user}");
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-console-auth-revoked", ("remaining", component.AuthorizationsRequired)));
        component.AuthorizedEntities.Clear();
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyRepeal(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null) return;

        if (!_idSystem.TryFindIdCard(player.Value, out var idCard) || !_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), player.Value, PopupType.Medium);
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Remove(MetaData(idCard.Owner).EntityName))
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-console-auth-revoked", ("remaining", remaining)));
        CheckForLaunch(component);
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null)
            return;

        if (!_idSystem.TryFindIdCard(player.Value, out var idCard) || !_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), args.Session, PopupType.Medium);
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(MetaData(idCard.Owner).EntityName))
            return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch AUTH by {args.Session:user}");
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
        _announced = false;
        _roundEndCancelToken = null;
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

        if (_uiSystem.TryGetUi(uid, EmergencyConsoleUiKey.Key, out var bui))
            _uiSystem.SetUiState(
                bui,
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

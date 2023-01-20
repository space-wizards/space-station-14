using System.Threading;
using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Server.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * Handles the emergency shuttle's console and early launching.
     */

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IdCardSystem _idSystem = default!;
    [Dependency] private readonly AccessReaderSystem _reader = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    /// <summary>
    /// Has the emergency shuttle arrived?
    /// </summary>
    public bool EmergencyShuttleArrived { get; private set; }

    public bool EarlyLaunchAuthorized { get; private set; }

    /// <summary>
    /// How much time remaining until the shuttle consoles for emergency shuttles are unlocked?
    /// </summary>
    private float _consoleAccumulator;

    /// <summary>
    /// How long after the transit is over to end the round.
    /// </summary>
    private readonly TimeSpan _bufferTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <see cref="CCVars.EmergencyShuttleTransitTime"/>
    /// </summary>
    public float TransitTime { get; private set; }

    /// <summary>
    /// <see cref="CCVars.EmergencyShuttleAuthorizeTime"/>
    /// </summary>
    private float _authorizeTime;

    private CancellationTokenSource? _roundEndCancelToken;

    private const string EmergencyRepealAllAccess = "EmergencyShuttleRepealAll";
    private static readonly Color DangerColor = Color.Red;

    /// <summary>
    /// Have the emergency shuttles been authorised to launch at CentCom?
    /// </summary>
    private bool _launchedShuttles;

    /// <summary>
    /// Have we announced the launch?
    /// </summary>
    private bool _announced;

    private void InitializeEmergencyConsole()
    {
        _configManager.OnValueChanged(CCVars.EmergencyShuttleTransitTime, SetTransitTime, true);
        _configManager.OnValueChanged(CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime, true);
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

    private void SetTransitTime(float obj)
    {
        TransitTime = obj;
    }

    private void ShutdownEmergencyConsole()
    {
        _configManager.UnsubValueChanged(CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime);
        _configManager.UnsubValueChanged(CCVars.EmergencyShuttleTransitTime, SetTransitTime);
    }

    private void OnEmergencyStartup(EntityUid uid, EmergencyShuttleConsoleComponent component, ComponentStartup args)
    {
        UpdateConsoleState(uid, component);
    }

    private void UpdateEmergencyConsole(float frameTime)
    {
        if (_consoleAccumulator <= 0f) return;

        _consoleAccumulator -= frameTime;

        // No early launch but we're under the timer.
        if (!_launchedShuttles && _consoleAccumulator <= _authorizeTime)
        {
            if (!EarlyLaunchAuthorized)
                AnnounceLaunch();
        }

        // Imminent departure
        if (!_launchedShuttles && _consoleAccumulator <= DefaultStartupTime)
        {
            _launchedShuttles = true;

            if (CentComMap != null)
            {
                foreach (var comp in EntityQuery<StationDataComponent>(true))
                {
                    if (!TryComp<ShuttleComponent>(comp.EmergencyShuttle, out var shuttle)) continue;

                    if (Deleted(CentCom))
                    {
                        // TODO: Need to get non-overlapping positions.
                        FTLTravel(shuttle,
                            new EntityCoordinates(
                                _mapManager.GetMapEntityId(CentComMap.Value),
                                Vector2.One * 1000f), _consoleAccumulator, TransitTime);
                    }
                    else
                    {
                        FTLTravel(shuttle,
                            CentCom.Value, _consoleAccumulator, TransitTime, dock: true);
                    }
                }
            }
        }

        // Departed
        if (_consoleAccumulator <= 0f)
        {
            _launchedShuttles = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-left", ("transitTime", $"{TransitTime:0}")));

            _roundEndCancelToken = new CancellationTokenSource();
            Timer.Spawn((int) (TransitTime * 1000) + _bufferTime.Milliseconds, () => _roundEnd.EndRound(), _roundEndCancelToken.Token);

            // Guarantees that emergency shuttle arrives first before anyone else can FTL.
            if (CentCom != null)
                AddFTLDestination(CentCom.Value, true);

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

        if (component.AuthorizedEntities.Count == 0) return;

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
        if (!component.AuthorizedEntities.Remove(MetaData(idCard.Owner).EntityName)) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("emergency-shuttle-console-auth-revoked", ("remaining", remaining)));
        CheckForLaunch(component);
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null) return;

        if (!_idSystem.TryFindIdCard(player.Value, out var idCard) || !_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), args.Session, PopupType.Medium);
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(MetaData(idCard.Owner).EntityName)) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch AUTH by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;

        if (remaining > 0)
            _chatSystem.DispatchGlobalAnnouncement(
                Loc.GetString("emergency-shuttle-console-auth-left", ("remaining", remaining)),
                playSound: false, colorOverride: DangerColor);

        if (!CheckForLaunch(component))
            SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());

        UpdateAllEmergencyConsoles();
    }

    private void CleanupEmergencyConsole()
    {
        _announced = false;
        _roundEndCancelToken = null;
        _launchedShuttles = false;
        _consoleAccumulator = 0f;
        EarlyLaunchAuthorized = false;
        EmergencyShuttleArrived = false;
    }

    private void UpdateAllEmergencyConsoles()
    {
        foreach (var comp in EntityQuery<EmergencyShuttleConsoleComponent>(true))
        {
            UpdateConsoleState(comp.Owner, comp);
        }
    }

    private void UpdateConsoleState(EntityUid uid, EmergencyShuttleConsoleComponent component)
    {
        var auths = new List<string>();

        foreach (var auth in component.AuthorizedEntities)
        {
            auths.Add(auth);
        }

        _uiSystem.GetUiOrNull(uid, EmergencyConsoleUiKey.Key)?.SetState(new EmergencyConsoleBoundUserInterfaceState()
        {
            EarlyLaunchTime = EarlyLaunchAuthorized ? _timing.CurTime + TimeSpan.FromSeconds(_consoleAccumulator) : null,
            Authorizations = auths,
            AuthorizationsRequired = component.AuthorizationsRequired,
        });
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
        _consoleAccumulator =_authorizeTime;
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

        SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());
    }

    public bool DelayEmergencyRoundEnd()
    {
        if (_roundEndCancelToken == null) return false;
        _roundEndCancelToken?.Cancel();
        _roundEndCancelToken = null;
        return true;
    }
}

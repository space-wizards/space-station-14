using System.Threading;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
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
    private float _transitTime;

    /// <summary>
    /// <see cref="CCVars.EmergencyShuttleAuthorizeTime"/>
    /// </summary>
    private float _authorizeTime;

    private CancellationTokenSource? _roundEndCancelToken;

    private const string EmergencyRepealAllAccess = "EmergencyShuttleRepealAll";
    private static readonly Color DangerColor = Color.Red;

    /// <summary>
    /// Have the emergency shuttles been authorised to launch at Centcomm?
    /// </summary>
    private bool _launchedShuttles;

    private void InitializeEmergencyConsole()
    {
        _configManager.OnValueChanged(CCVars.EmergencyShuttleTransitTime, SetTransitTime, true);
        _configManager.OnValueChanged(CCVars.EmergencyShuttleAuthorizeTime, SetAuthorizeTime, true);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, ComponentStartup>(OnEmergencyStartup);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
    }

    private void SetAuthorizeTime(float obj)
    {
        _authorizeTime = obj;
    }

    private void SetTransitTime(float obj)
    {
        _transitTime = obj;
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

        if (!_launchedShuttles && _consoleAccumulator <= DefaultStartupTime)
        {
            _launchedShuttles = true;

            if (_centcommMap != null)
            {
                foreach (var comp in EntityQuery<StationDataComponent>(true))
                {
                    if (!TryComp<ShuttleComponent>(comp.EmergencyShuttle, out var shuttle)) continue;

                    if (Deleted(_centcomm))
                    {
                        // TODO: Need to get non-overlapping positions.
                        Hyperspace(shuttle,
                            new EntityCoordinates(
                                _mapManager.GetMapEntityId(_centcommMap.Value),
                                Vector2.One * 1000f), _consoleAccumulator, _transitTime);
                    }
                    else
                    {
                        Hyperspace(shuttle,
                            _centcomm.Value, _consoleAccumulator, _transitTime);
                    }
                }
            }
        }

        if (_consoleAccumulator <= 0f)
        {
            _launchedShuttles = true;
            _chatSystem.DispatchGlobalStationAnnouncement(Loc.GetString("emergency-shuttle-left", ("transitTime", $"{_transitTime:0}")));

            _roundEndCancelToken = new CancellationTokenSource();
            Timer.Spawn((int) (_transitTime * 1000) + _bufferTime.Milliseconds, () => _roundEnd.EndRound(), _roundEndCancelToken.Token);
        }
    }

    private void OnEmergencyRepealAll(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealAllMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null) return;

        if (!_reader.FindAccessTags(player.Value).Contains(EmergencyRepealAllAccess))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), Filter.Entities(player.Value));
            return;
        }

        if (component.AuthorizedEntities.Count == 0) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL ALL by {args.Session:user}");
        component.AuthorizedEntities.Clear();
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyRepeal(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null) return;

        if (!_reader.IsAllowed(player.Value, uid))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Remove(MetaData(player.Value).EntityName)) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch REPEAL by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;
        _chatSystem.DispatchGlobalStationAnnouncement(Loc.GetString("emergency-shuttle-console-auth-revoked", ("remaining", remaining)));
        CheckForLaunch(component);
        UpdateAllEmergencyConsoles();
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Session.AttachedEntity;
        if (player == null) return;

        if (!_reader.IsAllowed(player.Value, uid))
        {
            _popup.PopupCursor(Loc.GetString("emergency-shuttle-console-denied"), Filter.Entities(player.Value));
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(MetaData(player.Value).EntityName)) return;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle early launch AUTH by {args.Session:user}");
        var remaining = component.AuthorizationsRequired - component.AuthorizedEntities.Count;

        if (remaining > 0)
            _chatSystem.DispatchGlobalStationAnnouncement(
                Loc.GetString("emergency-shuttle-console-auth-left", ("remaining", remaining)),
                playDefaultSound: false, colorOverride: DangerColor);

        if (!CheckForLaunch(component))
            SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());

        UpdateAllEmergencyConsoles();
    }

    private void CleanupEmergencyConsole()
    {
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
        if (EarlyLaunchAuthorized || !EmergencyShuttleArrived) return false;

        _logger.Add(LogType.EmergencyShuttle, LogImpact.Extreme, $"Emergency shuttle launch authorized");
        _consoleAccumulator = MathF.Max(1f, MathF.Min(_consoleAccumulator, _authorizeTime));
        EarlyLaunchAuthorized = true;
        RaiseLocalEvent(new EmergencyShuttleAuthorizedEvent());
        _chatSystem.DispatchGlobalStationAnnouncement(
            Loc.GetString("emergency-shuttle-launch-time", ("consoleAccumulator", $"{_consoleAccumulator:0}")),
            playDefaultSound: false,
            colorOverride: DangerColor);

        SoundSystem.Play("/Audio/Misc/notice1.ogg", Filter.Broadcast());
        UpdateAllEmergencyConsoles();
        return true;
    }

    public bool DelayEmergencyRoundEnd()
    {
        if (_roundEndCancelToken == null) return false;
        _roundEndCancelToken = null;
        _roundEndCancelToken?.Cancel();
        return true;
    }
}

using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * Handles the emergency shuttle's console and early launching.
     */

    [Dependency] private readonly AccessReaderSystem _reader = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <summary>
    /// Has the emergency shuttle arrived?
    /// </summary>
    public bool EmergencyShuttleArrived { get; private set; }

    public bool EmergencyShuttleAuthorized { get; private set; }

    /// <summary>
    /// How much time remaining until the shuttle consoles for emergency shuttles are unlocked?
    /// </summary>
    private float _accumulator;

    // TODO: CVar
    /// <summary>
    /// How long it takes for an emergency shuttle console to be unlocked.
    /// </summary>
    public TimeSpan EmergencyShuttleDefaultTime = TimeSpan.FromSeconds(5);

    private void InitializeEmergencyConsole()
    {
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
    }

    private void UpdateEmergencyConsole(float frameTime)
    {
        if (_accumulator <= 0f) return;

        _accumulator -= frameTime;

        // TODO: Authorise shuttles NOW! SLOTH! DO IT NOW!
        if (_accumulator <= frameTime)
        {

        }
    }

    private void OnEmergencyRepealAll(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealAllMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        if (!_reader.FindAccessTags(idCard.Owner).Contains("EmergencyShuttleRepealAll"))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        if (component.AuthorizedEntities.Count == 0) return;

        component.AuthorizedEntities.Clear();

        // TODO: Authorize
        UpdateConsoleState(uid, component);
    }

    private void OnEmergencyRepeal(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        if (!_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Remove(idCard.FullName ?? idCard.OriginalOwnerName)) return;

        CheckForLaunch(component);

        // TODO: Authorize
        UpdateConsoleState(uid, component);
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        if (!_reader.IsAllowed(idCard.Owner, uid))
        {
            _popup.PopupCursor("Access denied", Filter.Entities(player.Value));
            return;
        }

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(idCard.FullName ?? idCard.OriginalOwnerName)) return;

        CheckForLaunch(component);

        // TODO: Authorize
        UpdateConsoleState(uid, component);
    }

    private void CleanupEmergencyConsole()
    {
        EmergencyShuttleAuthorized = false;
        EmergencyShuttleArrived = false;
    }

    private void UpdateConsoleState(EntityUid uid, EmergencyShuttleConsoleComponent component)
    {
        var auths = new List<string>();

        foreach (var auth in component.AuthorizedEntities)
        {
            auths.Add(auth);
        }

        _uiSystem.GetUiOrNull(uid, EmergencyShuttleConsoleUiKey.Key)?.SetState(new EmergencyShuttleConsoleBoundUserInterfaceState()
        {
            Authorizations = auths,
            AuthorizationsRequired = component.AuthorizationsRequired,
        });
    }

    private void CheckForLaunch(EmergencyShuttleConsoleComponent component)
    {
        if (component.AuthorizedEntities.Count < component.AuthorizationsRequired || EmergencyShuttleAuthorized)
            return;

        EmergencyShuttleAuthorized = true;
        RaiseLocalEvent(new EmergencyShuttleAuthorizedEvent());
    }
}

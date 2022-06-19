using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    public bool EmergencyShuttleAuthorized { get; private set; }

    private void InitializeEmergencyConsole()
    {
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleAuthorizeMessage>(OnEmergencyAuthorize);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealMessage>(OnEmergencyRepeal);
        SubscribeLocalEvent<EmergencyShuttleConsoleComponent, EmergencyShuttleRepealAllMessage>(OnEmergencyRepealAll);
    }

    private void OnEmergencyRepealAll(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealAllMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        // TODO: Check for access

        if (component.AuthorizedEntities.Count == 0) return;

        component.AuthorizedEntities.Clear();

        // TODO: Authorize
        UpdateConsole(uid, component);
    }

    private void OnEmergencyRepeal(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleRepealMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        // TODO: Check for access

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Remove(idCard.FullName ?? idCard.OriginalOwnerName)) return;

        CheckForLaunch(component);

        // TODO: Authorize
        UpdateConsole(uid, component);
    }

    private void OnEmergencyAuthorize(EntityUid uid, EmergencyShuttleConsoleComponent component, EmergencyShuttleAuthorizeMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands)) return;

        var activeEnt = hands.ActiveHandEntity;

        if (activeEnt == null ||
            !_idCard.TryGetIdCard(activeEnt.Value, out var idCard)) return;

        // TODO: Check for access

        // TODO: This is fucking bad
        if (!component.AuthorizedEntities.Add(idCard.FullName ?? idCard.OriginalOwnerName)) return;

        CheckForLaunch(component);

        // TODO: Authorize
        UpdateConsole(uid, component);
    }

    private void CleanupEmergencyConsole()
    {
        EmergencyShuttleAuthorized = false;
    }

    private void UpdateConsole(EntityUid uid, EmergencyShuttleConsoleComponent component)
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

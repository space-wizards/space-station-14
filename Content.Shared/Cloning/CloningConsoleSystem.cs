using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning.CloningConsole;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.MedicalScanner.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.Player;

namespace Content.Shared.Cloning;

/// <summary>
/// Handles the logic and interactions for the cloning console,
/// enabling management of cloning pods and cloning operations.
/// </summary>
public sealed class CloningConsoleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCloningPodSystem _cloningPod = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CloningConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CloningConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
        SubscribeLocalEvent<CloningConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<CloningConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CloningConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CloningConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<CloningConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<CloningConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnInit(EntityUid uid, CloningConsoleComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, CloningConsoleComponent.ScannerPort, CloningConsoleComponent.PodPort);
    }

    private void OnButtonPressed(EntityUid uid, CloningConsoleComponent consoleComponent, UiButtonPressedMessage args)
    {
        if (!_powerReceiver.IsPowered(uid))
            return;

        switch (args.Button)
        {
            case UiButton.Clone:
                if (consoleComponent.GeneticScanner != null && consoleComponent.CloningPod != null)
                    TryClone(uid, consoleComponent.CloningPod.Value, consoleComponent.GeneticScanner.Value, consoleComponent: consoleComponent);
                break;
        }

        UpdateUserInterface(uid, consoleComponent);
    }

    private void OnPowerChanged(EntityUid uid, CloningConsoleComponent component, ref PowerChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnMapInit(EntityUid uid, CloningConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
            return;

        foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
        {
            if (TryComp<MedicalScannerComponent>(port, out var scanner))
            {
                component.GeneticScanner = port;
                scanner.ConnectedConsole = uid;
                Dirty(port, scanner);
            }

            if (TryComp<CloningPodComponent>(port, out var pod))
            {
                component.CloningPod = port;
                pod.ConnectedConsole = uid;
                Dirty(port, pod);
            }
        }
    }

    private void OnNewLink(EntityUid uid, CloningConsoleComponent component, NewLinkEvent args)
    {
        if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner) && args.SourcePort == CloningConsoleComponent.ScannerPort)
        {
            component.GeneticScanner = args.Sink;
            scanner.ConnectedConsole = uid;
            Dirty(args.Sink, scanner);
        }

        if (TryComp<CloningPodComponent>(args.Sink, out var pod) && args.SourcePort == CloningConsoleComponent.PodPort)
        {
            component.CloningPod = args.Sink;
            pod.ConnectedConsole = uid;
            Dirty(args.Sink, pod);
        }

        Dirty(uid, component);
        RecheckConnections(uid, component.CloningPod, component.GeneticScanner, component);
    }

    private void OnPortDisconnected(EntityUid uid, CloningConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port == CloningConsoleComponent.ScannerPort)
            component.GeneticScanner = null;

        if (args.Port == CloningConsoleComponent.PodPort)
            component.CloningPod = null;

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpen(EntityUid uid, CloningConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnAnchorChanged(EntityUid uid, CloningConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            RecheckConnections(uid, component.CloningPod, component.GeneticScanner, component);
            return;
        }

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Updates the state of the user interface for the cloning console.
    /// </summary>
    public void UpdateUserInterface(EntityUid consoleUid, CloningConsoleComponent consoleComponent)
    {
        if (!_ui.HasUi(consoleUid, CloningConsoleUiKey.Key))
            return;

        if (!_powerReceiver.IsPowered(consoleUid))
        {
            _ui.CloseUis(consoleUid);
            return;
        }

        var newState = GetUserInterfaceState(consoleComponent);
        _ui.SetUiState(consoleUid, CloningConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Attempts to start a cloning operation given the relevant entities.
    /// </summary>
    public void TryClone(EntityUid uid, EntityUid cloningPodUid, EntityUid scannerUid, CloningPodComponent? cloningPod = null, MedicalScannerComponent? scannerComp = null, CloningConsoleComponent? consoleComponent = null)
    {
        if (!Resolve(uid, ref consoleComponent) || !Resolve(cloningPodUid, ref cloningPod) || !Resolve(scannerUid, ref scannerComp))
            return;

        if (!Transform(cloningPodUid).Anchored || !Transform(scannerUid).Anchored)
            return;

        if (!consoleComponent.CloningPodInRange || !consoleComponent.GeneticScannerInRange)
            return;

        var body = scannerComp.BodyContainer.ContainedEntity;

        if (body is null)
            return;

        if (!_mind.TryGetMind(body.Value, out var mindId, out var mind))
            return;

        if (mind.UserId.HasValue == false || !_playerManager.ValidSessionId(mind.UserId.Value))
            return;

        if (_cloningPod.TryCloning(cloningPodUid, body.Value, (mindId, mind), cloningPod, scannerComp.CloningFailChanceMultiplier))
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(uid)} successfully cloned {ToPrettyString(body.Value)}.");
    }

    /// <summary>
    /// Checks distance connections between the console and machines, and updates states accordingly.
    /// </summary>
    public void RecheckConnections(EntityUid console, EntityUid? cloningPod, EntityUid? scanner, CloningConsoleComponent? consoleComp = null)
    {
        if (!Resolve(console, ref consoleComp))
            return;

        if (scanner != null && Exists(scanner.Value))
        {
            Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out var scannerDistance);
            consoleComp.GeneticScannerInRange = scannerDistance <= consoleComp.MaxDistance;
        }

        if (cloningPod != null && Exists(cloningPod.Value))
        {
            Transform(cloningPod.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out var podDistance);
            consoleComp.CloningPodInRange = podDistance <= consoleComp.MaxDistance;
        }

        Dirty(console, consoleComp);
        UpdateUserInterface(console, consoleComp);
    }
    private CloningConsoleBoundUserInterfaceState GetUserInterfaceState(CloningConsoleComponent consoleComponent)
    {
        var clonerStatus = ClonerStatus.Ready;

        // Genetic scanner info.
        var scanBodyInfo = Loc.GetString("generic-unknown");
        var scannerConnected = false;
        var scannerInRange = consoleComponent.GeneticScannerInRange;
        if (consoleComponent.GeneticScanner != null && TryComp<MedicalScannerComponent>(consoleComponent.GeneticScanner, out var scanner))
        {
            scannerConnected = true;
            EntityUid? scanBody = null;
            if (scanner.BodyContainer != null)
                scanBody = scanner.BodyContainer.ContainedEntity;

            // Get state.
            if (scanBody == null || !HasComp<MobStateComponent>(scanBody))
            {
                clonerStatus = ClonerStatus.ScannerEmpty;
            }
            else
            {
                scanBodyInfo = MetaData(scanBody.Value).EntityName;

                if (!_mobState.IsDead(scanBody.Value))
                {
                    clonerStatus = ClonerStatus.ScannerOccupantAlive;
                }
                else
                {
                    if (!_mind.TryGetMind(scanBody.Value, out _, out var mind) ||
                        mind.UserId == null ||
                        !_playerManager.TryGetSessionById(mind.UserId.Value, out _))
                    {
                        clonerStatus = ClonerStatus.NoMindDetected;
                    }
                }
            }
        }

        // Cloning pod info.
        var cloneBodyInfo = Loc.GetString("generic-unknown");
        var clonerConnected = false;
        var clonerMindPresent = false;
        var clonerInRange = consoleComponent.CloningPodInRange;
        if (consoleComponent.CloningPod != null && TryComp<CloningPodComponent>(consoleComponent.CloningPod, out var clonePod)
        && Transform(consoleComponent.CloningPod.Value).Anchored)
        {
            clonerConnected = true;
            EntityUid? cloneBody = null;
            if (clonePod.BodyContainer != null)
                cloneBody = clonePod.BodyContainer.ContainedEntity;

            clonerMindPresent = clonePod.Status == CloningPodStatus.Cloning;
            if (HasComp<ActiveCloningPodComponent>(consoleComponent.CloningPod))
            {
                if (cloneBody != null)
                    cloneBodyInfo = Identity.Name(cloneBody.Value, EntityManager);
                clonerStatus = ClonerStatus.ClonerOccupied;
            }
        }
        else
        {
            clonerStatus = ClonerStatus.NoClonerDetected;
        }

        return new CloningConsoleBoundUserInterfaceState(
            scanBodyInfo,
            cloneBodyInfo,
            clonerMindPresent,
            clonerStatus,
            scannerConnected,
            scannerInRange,
            clonerConnected,
            clonerInRange
            );
    }

}

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

    private void OnInit(Entity<CloningConsoleComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, CloningConsoleComponent.ScannerPort, CloningConsoleComponent.PodPort);
    }

    private void OnButtonPressed(Entity<CloningConsoleComponent> ent, ref UiButtonPressedMessage args)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        switch (args.Button)
        {
            case UiButton.Clone:
                if (ent.Comp.GeneticScanner != null && ent.Comp.CloningPod != null)
                    TryClone(ent.Owner, ent.Comp.CloningPod.Value, ent.Comp.GeneticScanner.Value);
                break;
        }

        UpdateUserInterface(ent);
    }

    private void OnPowerChanged(Entity<CloningConsoleComponent> ent, ref PowerChangedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnMapInit(Entity<CloningConsoleComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(ent.Owner, out var receiver))
            return;

        foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
        {
            if (TryComp<MedicalScannerComponent>(port, out var scanner))
            {
                ent.Comp.GeneticScanner = port;
                scanner.ConnectedConsole = ent.Owner;
                Dirty(port, scanner);
            }

            if (TryComp<CloningPodComponent>(port, out var pod))
            {
                ent.Comp.CloningPod = port;
                pod.ConnectedConsole = ent.Owner;
                Dirty(port, pod);
            }
        }
    }

    private void OnNewLink(Entity<CloningConsoleComponent> ent, ref NewLinkEvent args)
    {
        if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner) && args.SourcePort == CloningConsoleComponent.ScannerPort)
        {
            ent.Comp.GeneticScanner = args.Sink;
            scanner.ConnectedConsole = ent.Owner;
            Dirty(args.Sink, scanner);
        }

        if (TryComp<CloningPodComponent>(args.Sink, out var pod) && args.SourcePort == CloningConsoleComponent.PodPort)
        {
            ent.Comp.CloningPod = args.Sink;
            pod.ConnectedConsole = ent.Owner;
            Dirty(args.Sink, pod);
        }

        Dirty(ent);
        RecheckConnections(ent.Owner, ent.Comp.CloningPod, ent.Comp.GeneticScanner);
    }

    private void OnPortDisconnected(Entity<CloningConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == CloningConsoleComponent.ScannerPort)
            ent.Comp.GeneticScanner = null;

        if (args.Port == CloningConsoleComponent.PodPort)
            ent.Comp.CloningPod = null;

        Dirty(ent);
        UpdateUserInterface(ent);
    }

    private void OnUIOpen(Entity<CloningConsoleComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnAnchorChanged(Entity<CloningConsoleComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            RecheckConnections(ent.Owner, ent.Comp.CloningPod, ent.Comp.GeneticScanner);
            return;
        }

        UpdateUserInterface(ent);
    }

    /// <summary>
    /// Updates the state of the user interface for the cloning console.
    /// </summary>
    public void UpdateUserInterface(Entity<CloningConsoleComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, CloningConsoleUiKey.Key))
            return;

        if (!_powerReceiver.IsPowered(ent.Owner))
        {
            _ui.CloseUis(ent.Owner);
            return;
        }

        var newState = GetUserInterfaceState(ent.Comp);
        _ui.SetUiState(ent.Owner, CloningConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Attempts to start a cloning operation given the relevant entities.
    /// </summary>
    public void TryClone(Entity<CloningConsoleComponent?> ent, Entity<CloningPodComponent?> entPod, Entity<MedicalScannerComponent?> entScanner)
    {
        if (!Resolve(ent.Owner, ref ent.Comp) || !Resolve(entPod.Owner, ref entPod.Comp) || !Resolve(entScanner.Owner, ref entScanner.Comp))
            return;

        if (!Transform(entPod.Owner).Anchored || !Transform(entScanner.Owner).Anchored)
            return;

        if (!ent.Comp.CloningPodInRange || !ent.Comp.GeneticScannerInRange)
            return;

        var body = entScanner.Comp.BodyContainer.ContainedEntity;

        if (body is null)
            return;

        if (!_mind.TryGetMind(body.Value, out var mindId, out var mind))
            return;

        if (mind.UserId.HasValue == false || !_playerManager.ValidSessionId(mind.UserId.Value))
            return;

        if (_cloningPod.TryCloning(entPod, body.Value, (mindId, mind), entScanner.Comp.CloningFailChanceMultiplier))
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner)} successfully cloned {ToPrettyString(body.Value)}.");
    }

    /// <summary>
    /// Checks distance connections between the console and machines, and updates states accordingly.
    /// </summary>
    public void RecheckConnections(Entity<CloningConsoleComponent?> ent, EntityUid? cloningPod, EntityUid? scanner)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (scanner != null && Exists(scanner.Value))
        {
            Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform(ent.Owner).Coordinates, out var scannerDistance);
            ent.Comp.GeneticScannerInRange = scannerDistance <= ent.Comp.MaxDistance;
        }

        if (cloningPod != null && Exists(cloningPod.Value))
        {
            Transform(cloningPod.Value).Coordinates.TryDistance(EntityManager, Transform(ent.Owner).Coordinates, out var podDistance);
            ent.Comp.CloningPodInRange = podDistance <= ent.Comp.MaxDistance;
        }

        Dirty(ent);
        UpdateUserInterface((ent.Owner, ent.Comp));
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

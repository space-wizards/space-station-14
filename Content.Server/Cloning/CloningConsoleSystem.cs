using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Cloning.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Medical.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Cloning;
using Content.Shared.Cloning.CloningConsole;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Cloning
{
    [UsedImplicitly]
    public sealed class CloningConsoleSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;

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
            _signalSystem.EnsureSourcePorts(uid, CloningConsoleComponent.ScannerPort, CloningConsoleComponent.PodPort);
        }
        private void OnButtonPressed(EntityUid uid, CloningConsoleComponent consoleComponent, UiButtonPressedMessage args)
        {
            if (!_powerReceiverSystem.IsPowered(uid))
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
                }

                if (TryComp<CloningPodComponent>(port, out var pod))
                {
                    component.CloningPod = port;
                    pod.ConnectedConsole = uid;
                }
            }
        }

        private void OnNewLink(EntityUid uid, CloningConsoleComponent component, NewLinkEvent args)
        {
            if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner) && args.SourcePort == CloningConsoleComponent.ScannerPort)
            {
                component.GeneticScanner = args.Sink;
                scanner.ConnectedConsole = uid;
            }

            if (TryComp<CloningPodComponent>(args.Sink, out var pod) && args.SourcePort == CloningConsoleComponent.PodPort)
            {
                component.CloningPod = args.Sink;
                pod.ConnectedConsole = uid;
            }
            RecheckConnections(uid, component.CloningPod, component.GeneticScanner, component);
        }

        private void OnPortDisconnected(EntityUid uid, CloningConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port == CloningConsoleComponent.ScannerPort)
                component.GeneticScanner = null;

            if (args.Port == CloningConsoleComponent.PodPort)
                component.CloningPod = null;

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

        public void UpdateUserInterface(EntityUid consoleUid, CloningConsoleComponent consoleComponent)
        {
            if (!_uiSystem.HasUi(consoleUid, CloningConsoleUiKey.Key))
                return;

            if (!_powerReceiverSystem.IsPowered(consoleUid))
            {
                _uiSystem.CloseUis(consoleUid);
                return;
            }

            var newState = GetUserInterfaceState(consoleComponent);
            _uiSystem.SetUiState(consoleUid, CloningConsoleUiKey.Key, newState);
        }

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

            if (!_mindSystem.TryGetMind(body.Value, out var mindId, out var mind))
                return;

            if (mind.UserId.HasValue == false || mind.Session == null)
                return;

            if (_cloningSystem.TryCloning(cloningPodUid, body.Value, (mindId, mind), cloningPod, scannerComp.CloningFailChanceMultiplier))
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(uid)} successfully cloned {ToPrettyString(body.Value)}.");
        }

        public void RecheckConnections(EntityUid console, EntityUid? cloningPod, EntityUid? scanner, CloningConsoleComponent? consoleComp = null)
        {
            if (!Resolve(console, ref consoleComp))
                return;

            if (scanner != null)
            {
                Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform((console)).Coordinates, out float scannerDistance);
                consoleComp.GeneticScannerInRange = scannerDistance <= consoleComp.MaxDistance;
            }
            if (cloningPod != null)
            {
                Transform(cloningPod.Value).Coordinates.TryDistance(EntityManager, Transform((console)).Coordinates, out float podDistance);
                consoleComp.CloningPodInRange = podDistance <= consoleComp.MaxDistance;
            }

            UpdateUserInterface(console, consoleComp);
        }
        private CloningConsoleBoundUserInterfaceState GetUserInterfaceState(CloningConsoleComponent consoleComponent)
        {
            ClonerStatus clonerStatus = ClonerStatus.Ready;

            // genetic scanner info
            string scanBodyInfo = Loc.GetString("generic-unknown");
            bool scannerConnected = false;
            bool scannerInRange = consoleComponent.GeneticScannerInRange;
            if (consoleComponent.GeneticScanner != null && TryComp<MedicalScannerComponent>(consoleComponent.GeneticScanner, out var scanner))
            {
                scannerConnected = true;
                EntityUid? scanBody = scanner.BodyContainer.ContainedEntity;

                // GET STATE
                if (scanBody == null || !HasComp<MobStateComponent>(scanBody))
                    clonerStatus = ClonerStatus.ScannerEmpty;
                else
                {
                    scanBodyInfo = MetaData(scanBody.Value).EntityName;

                    if (!_mobStateSystem.IsDead(scanBody.Value))
                    {
                        clonerStatus = ClonerStatus.ScannerOccupantAlive;
                    }
                    else
                    {
                        if (!_mindSystem.TryGetMind(scanBody.Value, out _, out var mind) ||
                            mind.UserId == null ||
                            !_playerManager.TryGetSessionById(mind.UserId.Value, out _))
                        {
                            clonerStatus = ClonerStatus.NoMindDetected;
                        }
                    }
                }
            }

            // cloning pod info
            var cloneBodyInfo = Loc.GetString("generic-unknown");
            bool clonerConnected = false;
            bool clonerMindPresent = false;
            bool clonerInRange = consoleComponent.CloningPodInRange;
            if (consoleComponent.CloningPod != null && TryComp<CloningPodComponent>(consoleComponent.CloningPod, out var clonePod)
            && Transform(consoleComponent.CloningPod.Value).Anchored)
            {
                clonerConnected = true;
                EntityUid? cloneBody = clonePod.BodyContainer.ContainedEntity;

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
}

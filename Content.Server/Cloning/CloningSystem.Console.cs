using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.Medical.Components;
using Robust.Shared.Map;
using Content.Server.Cloning.Components;
using Content.Server.Power.Components;
using Content.Server.Mind.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Content.Shared.ActionBlocker;
using Content.Shared.Cloning.CloningConsole;
using Content.Shared.Cloning;

namespace Content.Server.Cloning.Systems
{
    [UsedImplicitly]
    public sealed partial class CloningConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        private const float UpdateRate = 2f;
        private float _updateDif;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningConsoleComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<CloningConsoleComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<CloningConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
            SubscribeLocalEvent<CloningConsoleComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<CloningConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<CloningConsoleComponent, ComponentShutdown>(OnComponentShutdown);
        }

        private void OnComponentStartup(EntityUid uid, CloningConsoleComponent consoleComponent, ComponentStartup args)
        {
            FindDevices(uid, consoleComponent);
        }

        private void HandleActivateInWorld(EntityUid uid, CloningConsoleComponent consoleComponent, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            if (!IsPowered(consoleComponent))
                return;

            if (!_actionBlockerSystem.CanInteract(args.User, consoleComponent.Owner))
                return;

            _uiSystem.GetUiOrNull(consoleComponent.Owner, CloningConsoleUiKey.Key)?.Open(actor.PlayerSession);
        }

        private void OnButtonPressed(EntityUid uid, CloningConsoleComponent consoleComponent, UiButtonPressedMessage args)
        {
            if (!IsPowered(consoleComponent))
                return;

            switch (args.Button)
            {
                case UiButton.Clone:
                    if (consoleComponent.GeneticScanner != null && consoleComponent.CloningPod != null)
                        TryClone(uid, consoleComponent.CloningPod.Value, consoleComponent.GeneticScanner.Value, consoleComponent: consoleComponent);
                    break;
                case UiButton.Eject:
                    if (consoleComponent.CloningPod != null)
                        TryEject(uid, consoleComponent.CloningPod.Value, consoleComponent: consoleComponent);
                    break;
            }
        }

        private void OnPowerChanged(EntityUid uid, CloningConsoleComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
                _uiSystem.GetUiOrNull(uid, CloningConsoleUiKey.Key)?.CloseAll();
        }

        private void OnAnchorChanged(EntityUid uid, CloningConsoleComponent consoleComponent, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                FindDevices(uid, consoleComponent);
            else
                DisconnectMachineConnections(uid, consoleComponent);
        }

        private void OnComponentShutdown(EntityUid uid, CloningConsoleComponent consoleComponent, ComponentShutdown args)
        {
            DisconnectMachineConnections(uid, consoleComponent);
        }

        public bool IsPowered(CloningConsoleComponent consoleComponent)
        {
            if (TryComp<TransformComponent>(consoleComponent.Owner, out var transform) && transform.Anchored && TryComp<ApcPowerReceiverComponent>(consoleComponent.Owner, out var receiver))
                return receiver.Powered;

            return false;
        }

        private void UpdateUserInterface(CloningConsoleComponent consoleComponent)
        {
            if (!IsPowered(consoleComponent))
            {
                _uiSystem.GetUiOrNull(consoleComponent.Owner, CloningConsoleUiKey.Key)?.CloseAll();
                return;
            }

            var newState = GetUserInterfaceState(consoleComponent);

            _uiSystem.GetUiOrNull(consoleComponent.Owner, CloningConsoleUiKey.Key)?.SetState(newState);
        }

        private void FindDevices(EntityUid Owner, CloningConsoleComponent cloneConsoleComp)
        {
            cloneConsoleComp.CloningPod = null;
            cloneConsoleComp.GeneticScanner = null;
            if (TryComp<TransformComponent>(Owner, out var transformComp) && transformComp.Anchored)
            {
                var grid = _mapManager.GetGrid(transformComp.GridID);
                var coords = transformComp.Coordinates;
                foreach (var entity in grid.GetCardinalNeighborCells(coords))
                {
                    if (TryComp<MedicalScannerComponent>(entity, out var geneticScanner) && geneticScanner.ConnectedConsole == null)
                    {
                        cloneConsoleComp.GeneticScanner = entity;
                        geneticScanner.ConnectedConsole = Owner;
                        continue;
                    }

                    if (TryComp<CloningPodComponent>(entity, out var cloningPod) && cloningPod.ConnectedConsole == null)
                    {
                        cloneConsoleComp.CloningPod = entity;
                        cloningPod.ConnectedConsole = Owner;
                    }
                }
            }
            UpdateUserInterface(cloneConsoleComp);
            return;
        }

        public void TryEject(EntityUid uid, EntityUid clonePodUid, CloningPodComponent? cloningPod = null, CloningConsoleComponent? consoleComponent = null)
        {
            if (!Resolve(uid, ref consoleComponent) || !Resolve(clonePodUid, ref cloningPod))
                return;

            _cloningSystem.Eject(clonePodUid, cloningPod);
        }

        public void TryClone(EntityUid uid, EntityUid cloningPodUid, EntityUid scannerUid, CloningPodComponent? cloningPod = null, MedicalScannerComponent? scannerComp = null, CloningConsoleComponent? consoleComponent = null)
        {
            if (!Resolve(uid, ref consoleComponent) || !Resolve(cloningPodUid, ref cloningPod)  || !Resolve(scannerUid, ref scannerComp))
                return;

            if (scannerComp.BodyContainer.ContainedEntity is null)
                return;

            if (!TryComp<MindComponent>(scannerComp.BodyContainer.ContainedEntity.Value, out var mindComp))
                return;

            var mind = mindComp.Mind;

            if (mind == null || mind.UserId.HasValue == false || mind.Session == null)
                return;

            bool cloningSuccessful = _cloningSystem.TryCloning(cloningPodUid, scannerComp.BodyContainer.ContainedEntity.Value, mind, cloningPod);
        }

        private CloningConsoleBoundUserInterfaceState GetUserInterfaceState(CloningConsoleComponent consoleComponent)
        {
            ClonerStatus clonerStatus = ClonerStatus.Ready;

            // genetic scanner info
            string scanBodyInfo = "Unknown";
            bool scannerConnected = false;
            if (consoleComponent.GeneticScanner != null && TryComp<MedicalScannerComponent>(consoleComponent.GeneticScanner, out var scanner)) {

                scannerConnected = true;
                EntityUid? scanBody = scanner.BodyContainer.ContainedEntity;

                // GET NAME
                if (TryComp<MetaDataComponent>(scanBody, out var scanMetaData))
                    scanBodyInfo = scanMetaData.EntityName;

                // GET STATE
                if (scanBody == null)
                    clonerStatus = ClonerStatus.ScannerEmpty;
                else if (TryComp<MobStateComponent>(scanBody, out var mobState))
                {
                    TryComp<MindComponent>(scanBody, out var mindComp);

                    if (!mobState.IsDead())
                    {
                        clonerStatus = ClonerStatus.ScannerOccupantAlive;
                    }
                    else
                    {
                        if (mindComp == null || mindComp.Mind == null || mindComp.Mind.UserId == null || !_playerManager.TryGetSessionById(mindComp.Mind.UserId.Value, out var client))
                        {
                            clonerStatus = ClonerStatus.NoMindDetected;
                        }
                    }
                }
            }

            // cloning pod info
            var cloneBodyInfo = "Unknown";
            float cloningProgress = 0;
            float cloningTime = 30f;
            bool clonerProgressing = false;
            bool clonerConnected = false;
            bool clonerMindPresent = false;
            if (consoleComponent.CloningPod != null && TryComp<CloningPodComponent>(consoleComponent.CloningPod, out var clonePod))
            {
                clonerConnected = true;
                EntityUid? cloneBody = clonePod.BodyContainer.ContainedEntity;
                if (TryComp<MetaDataComponent>(cloneBody, out var cloneMetaData))
                    cloneBodyInfo = cloneMetaData.EntityName;

                cloningProgress = clonePod.CloningProgress;
                cloningTime = clonePod.CloningTime;
                clonerProgressing = _cloningSystem.IsPowered(clonePod) && (clonePod.BodyContainer.ContainedEntity != null);
                clonerMindPresent = clonePod.Status == CloningPodStatus.Cloning;
                if (cloneBody != null)
                {
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
                _gameTiming.CurTime,
                cloningProgress,
                cloningTime,
                clonerProgressing,
                clonerMindPresent,
                clonerStatus,
                scannerConnected,
                clonerConnected
                );
        }

        public void DisconnectMachineConnections(EntityUid uid, CloningConsoleComponent? consoleComponent)
        {
            if (!Resolve(uid, ref consoleComponent))
                return;

            if (consoleComponent.CloningPod != null && TryComp<CloningPodComponent>(consoleComponent.CloningPod, out var cloningPod) && cloningPod.ConnectedConsole == uid)
                cloningPod.ConnectedConsole = null;

            if (consoleComponent.GeneticScanner != null && TryComp<MedicalScannerComponent>(consoleComponent.GeneticScanner, out var medicalScanner) && medicalScanner.ConnectedConsole == uid)
                medicalScanner.ConnectedConsole = null;

            consoleComponent.CloningPod = null;
            consoleComponent.GeneticScanner = null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // check update rate
            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;
            _updateDif = 0f;

            var consoles = EntityManager.EntityQuery<CloningConsoleComponent>();
            foreach (var console in consoles)
            {
                UpdateUserInterface(console);
            }
        }
    }
}

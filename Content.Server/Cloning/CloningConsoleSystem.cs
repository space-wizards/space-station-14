using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.Medical.Components;
using Robust.Shared.Map;
using Content.Server.Cloning.Components;
using Content.Server.Power.Components;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Server.Player;
using Content.Shared.ActionBlocker;
using Content.Shared.Cloning.CloningConsole;
using Content.Shared.Cloning;

namespace Content.Server.Cloning.CloningConsole
{
    [UsedImplicitly]
    internal sealed class CloningConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        private const float UpdateRate = 3f;
        private float _updateDif;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningConsoleComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<CloningConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
            SubscribeLocalEvent<CloningConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void HandleActivateInWorld(EntityUid uid, CloningConsoleComponent consoleComponent, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            if (!IsPowered(consoleComponent))
                return;

            if (!_actionBlockerSystem.CanInteract(args.User, consoleComponent.Owner))
                return;

            consoleComponent.UserInterface?.Open(actor.PlayerSession);
        }

        private void OnButtonPressed(EntityUid uid, CloningConsoleComponent consoleComponent, UiButtonPressedMessage args)
        {
            if (!IsPowered(consoleComponent))
                return;

            switch (args.Button)
            {
                case UiButton.Clone:
                    TryClone(uid, consoleComponent);
                    break;
                case UiButton.Eject:
                    TryEject(uid, consoleComponent);
                    break;
                case UiButton.Refresh:
                    FindDevices(uid, consoleComponent);
                    break;
            }
        }

        private void OnPowerChanged(EntityUid uid, CloningConsoleComponent component, PowerChangedEvent args)
        {
            if (!args.Powered && component.UserInterface != null)
            {
                component.UserInterface.CloseAll();
            }
        }

        public bool IsPowered(CloningConsoleComponent consoleComponent)
        {
            if (TryComp<ApcPowerReceiverComponent>(consoleComponent.Owner, out var receiver))
                return receiver.Powered;

            return false;
        }

        private void UpdateUserInterface(CloningConsoleComponent consoleComponent)
        {
            if (!IsPowered(consoleComponent))
            {
                consoleComponent.UserInterface?.CloseAll();
                return;
            }
            var newState = GetUserInterfaceState(consoleComponent);
            if (newState == null)
                return;

            consoleComponent.UserInterface?.SetState(newState);
        }

        private void FindDevices(EntityUid Owner, CloningConsoleComponent cloneConsoleComp)
        {
            if (!EntityManager.EntityExists(cloneConsoleComp.CloningPod))
                cloneConsoleComp.CloningPod = null;

            if (!EntityManager.EntityExists(cloneConsoleComp.GeneticScanner))
                cloneConsoleComp.GeneticScanner = null;

            if (TryComp<TransformComponent>(Owner, out var transformComp) && transformComp.Anchored)
            {
                var grid = _mapManager.GetGrid(transformComp.GridID);
                var coords = transformComp.Coordinates;
                foreach (var entity in grid.GetCardinalNeighborCells(coords))
                {
                    if (TryComp<MedicalScannerComponent>(entity, out var geneticScanner))
                    {
                        cloneConsoleComp.GeneticScanner = entity;
                        continue;
                    }

                    if (TryComp<CloningPodComponent>(entity, out var cloningPod))
                        cloneConsoleComp.CloningPod = entity;
                }
            }
            return;
        }

        public void TryEject(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
            if (consoleComponent.CloningPod == null)
                return;

            if (!TryComp<CloningPodComponent>(consoleComponent.CloningPod, out var cloningPod))
            {
                consoleComponent.CloningPod = null;
                return;
            }

            _cloningSystem.Eject(consoleComponent.CloningPod.Value, cloningPod);
        }

        public void TryClone(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
             if (consoleComponent.GeneticScanner == null || consoleComponent.CloningPod == null)
                return;

            if (!TryComp<CloningPodComponent>(consoleComponent.CloningPod, out var cloningPod))
            {
                consoleComponent.CloningPod = null;
                return;
            }

             if (!TryComp<MedicalScannerComponent>(consoleComponent.GeneticScanner, out var scanner))
            {
                consoleComponent.GeneticScanner = null;
                return;
            }

            if (scanner.BodyContainer.ContainedEntity is null)
                return;

            if (!TryComp<MindComponent>(scanner.BodyContainer.ContainedEntity.Value, out var mindComp) || mindComp.Mind == null)
                return;

            var mind = mindComp.Mind!;
            var mindUser = mind.UserId;
            if (mindUser.HasValue == false || mind.Session == null)
                return;
            var profile = GetPlayerProfileAsync(mindUser.Value);
            bool cloningSuccessful = _cloningSystem.TryCloning(consoleComponent.CloningPod.Value, mind, profile, cloningPod);
            if (cloningSuccessful)
                consoleComponent.CloningHistory.Add(profile.Name);
        }

        private CloningConsoleBoundUserInterfaceState GetUserInterfaceState(CloningConsoleComponent consoleComponent)
        {
            ClonerStatusState clonerStatus = ClonerStatusState.Ready;

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
                    clonerStatus = ClonerStatusState.ScannerEmpty;
                else
                if (TryComp<MobStateComponent>(scanBody, out var mobState))
                {
                    TryComp<MindComponent>(scanBody, out var mindComp);

                    if (!mobState.IsDead())
                    {
                        clonerStatus = ClonerStatusState.ScannerOccupantAlive;
                    }
                    else
                    {
                        if (mindComp == null || mindComp.Mind == null || mindComp.Mind.UserId == null || !_playerManager.TryGetSessionById(mindComp.Mind.UserId.Value, out var client))
                        {
                            clonerStatus = ClonerStatusState.NoMindDetected;
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
                    clonerStatus = ClonerStatusState.ClonerOccupied;
                }
            }
            else
            {
                clonerStatus = ClonerStatusState.NoClonerDetected;
            }

            return new CloningConsoleBoundUserInterfaceState(
                scanBodyInfo,
                cloneBodyInfo,
                consoleComponent.CloningHistory,
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

        private HumanoidCharacterProfile GetPlayerProfileAsync(NetUserId userId)
        {
            return (HumanoidCharacterProfile)  _prefsManager.GetPreferences(userId).SelectedCharacter;
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

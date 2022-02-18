using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.Cloning.GeneticScanner;
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

using static Content.Shared.Cloning.SharedCloningPodComponent;
using static Content.Shared.Cloning.CloningConsole.SharedCloningConsoleComponent;

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

            SubscribeLocalEvent<CloningConsoleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<CloningConsoleComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<CloningConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
            SubscribeLocalEvent<CloningConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnComponentInit(EntityUid uid, CloningConsoleComponent comp, ComponentInit args)
        {
            var newState = GetUserInterfaceState(comp);
            comp.UserInterface?.SetState(newState);
            UpdateUserInterface(comp);
        }

        private void HandleActivateInWorld(EntityUid uid, CloningConsoleComponent consoleComponent, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
            {
                return;
            }
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

            if (args.Button == UiButton.Clone) {
                TryClone(uid, consoleComponent);
                return;
            }
            if (args.Button == UiButton.Eject) {
                TryEject(uid, consoleComponent);
                return;
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
            {
                return receiver.Powered;
            }
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
            if (!EntityManager.EntityExists(cloneConsoleComp.CloningPod?.Owner))
            {
                cloneConsoleComp.CloningPod = null;
            }
            if (EntityManager.EntityExists(cloneConsoleComp.GeneticScanner?.Owner))
            {
                cloneConsoleComp.GeneticScanner = null;
            }

            if (TryComp<TransformComponent>(Owner, out var transformComp) && transformComp.Anchored)
            {
                var grid = _mapManager.GetGrid(transformComp.GridID);
                var coords = transformComp.Coordinates;
                foreach (var entity in grid.GetCardinalNeighborCells(coords))
                {
                    if (TryComp<GeneticScannerComponent>(entity, out var geneticScanner))
                    {
                        cloneConsoleComp.GeneticScanner = geneticScanner;
                        continue;
                    }
                    if (TryComp<CloningPodComponent>(entity, out var cloningPod))
                    {
                        cloneConsoleComp.CloningPod = cloningPod;
                    }
                }
            }
            return;
        }

        public void TryEject(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
            if (consoleComponent.CloningPod == null)
                return;
            _cloningSystem.Eject(uid, consoleComponent.CloningPod);
        }

        public void TryClone(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
             if (consoleComponent.GeneticScanner != null && consoleComponent.CloningPod != null && consoleComponent.GeneticScanner.BodyContainer.ContainedEntity != null)
                {
                    if (!TryComp<MindComponent>(consoleComponent.GeneticScanner.BodyContainer.ContainedEntity.Value, out MindComponent? mindComp) || mindComp.Mind == null)
                        return;
                    var mind = mindComp.Mind!;
                    var mindUser = mind.UserId;
                    if (mindUser.HasValue == false || mind.Session == null)
                        return;

                    if (mindUser.HasValue == false || mind.Session == null)
                    {
                        return;
                    }
                    var profile = GetPlayerProfileAsync(mindUser.Value);
                    bool cloningSuccessful = _cloningSystem.TryCloning(mind, profile, consoleComponent.CloningPod);
                    if (cloningSuccessful)
                        consoleComponent.CloningHistory.Add(profile.Name);
                }
        }

        private CloningConsoleBoundUserInterfaceState GetUserInterfaceState(CloningConsoleComponent consoleComponent)
        {
            FindDevices(consoleComponent.Owner, consoleComponent);

            ClonerStatusState clonerStatus = ClonerStatusState.Ready;

            // genetic scanner info
            string scanBodyInfo = "Unknown";
            bool scannerConnected = false;
            if (consoleComponent.GeneticScanner != null) {
                scannerConnected = true;
                EntityUid? scanBody = consoleComponent.GeneticScanner.BodyContainer.ContainedEntity;
                // GET NAME
                if (TryComp<MetaDataComponent>(scanBody, out MetaDataComponent? scanMetaData))
                {
                    scanBodyInfo = scanMetaData.EntityName;
                }
                // GET STATE
                if (scanBody == null)
                {
                    clonerStatus = ClonerStatusState.ScannerEmpty;
                }
                else
                if (TryComp<MobStateComponent>(scanBody, out MobStateComponent? mobState))
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
            if (consoleComponent.CloningPod != null)
            {
                clonerConnected = true;
                EntityUid? cloneBody = consoleComponent.CloningPod.BodyContainer.ContainedEntity;
                if (TryComp<MetaDataComponent>(cloneBody, out var cloneMetaData))
                    cloneBodyInfo = cloneMetaData.EntityName;

                cloningProgress = consoleComponent.CloningPod.CloningProgress;
                cloningTime = consoleComponent.CloningPod.CloningTime;
                clonerProgressing = _cloningSystem.IsPowered(consoleComponent.CloningPod) && (consoleComponent.CloningPod.BodyContainer.ContainedEntity != null);
                clonerMindPresent = consoleComponent.CloningPod.Status == CloningPodStatus.Cloning;
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

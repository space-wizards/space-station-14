using Content.Server.Medical.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.GeneticScanner;
using Robust.Shared.Map;
using Content.Server.Cloning.Components;
using Content.Server.Power.Components;
using System;
using Content.Server.Climbing;
using Content.Server.Cloning;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Server.UserInterface;
using Content.Shared.Acts;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.CloningConsole;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using System.Collections.Generic;
using Content.Shared.Cloning;

using static Content.Shared.Cloning.SharedCloningPodComponent;
using static Content.Shared.CloningConsole.SharedCloningConsoleComponent;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class CloningConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;


        private const float UpdateRate = 3f;
        private float _updateDif;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningConsoleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<CloningConsoleComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<CloningConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
        }

        private void OnComponentInit(EntityUid uid, CloningConsoleComponent comp, ComponentInit args)
        {
            var newState = GetUserInterfaceState(comp);
            if (newState == null)
                return;
            comp.UserInterface?.SetState(newState);
            UpdateUserInterface(comp);
        }

        public void FindScanner(EntityUid Owner, CloningConsoleComponent? cloneConsoleComp = null)
        {
            if (!Resolve(Owner, ref cloneConsoleComp))
                return;

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

        private void HandleActivateInWorld(EntityUid uid, CloningConsoleComponent consoleComponent, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
            {
                return;
            }
            if (!IsPowered(consoleComponent))
                return;

            consoleComponent.UserInterface?.Open(actor.PlayerSession);
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

        public void TryEject(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
            if (consoleComponent.CloningPod == null)
                return;
            EntitySystem.Get<CloningPodSystem>().Eject(consoleComponent.CloningPod);
        }

        public void TryClone(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
             if (consoleComponent.GeneticScanner != null && consoleComponent.GeneticScanner.BodyContainer.ContainedEntity != null)
                {
                    var cloningPodSystem = EntitySystem.Get<CloningPodSystem>();
                    if (!TryComp<MindComponent>( consoleComponent.GeneticScanner.BodyContainer.ContainedEntity.Value, out MindComponent? mindComp) || mindComp.Mind == null)
                    {
                        // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-no-soul"));
                        // break;
                        return;
                    }
                    // Null suppression based on above check. Yes, it's explicitly needed
                    var mind = mindComp.Mind!;

                    // We need the HumanoidCharacterProfile
                    // TODO: Move this further 'outwards' into a DNAComponent or somesuch.
                    // Ideally this ends with GameTicker & CloningSystem handing DNA to a function that sets up a body for that DNA.
                    var mindUser = mind.UserId;
                    if (mindUser.HasValue == false || mind.Session == null)
                    {
                        // For now assume this means soul departed
                        // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-soul-broken"));
                        return;
                    }
                    // var profile = GetPlayerProfileAsync(mindUser.Value);
                    // cloningSystem.AddToDnaScans(new ClonerDNAEntry(mind, profile));
                    if (consoleComponent.CloningPod != null && consoleComponent.GeneticScanner != null)
                    {
                        // var mind = mindComp.Mind!;
                        // var mindUser = mind.UserId;
                        if (mindUser.HasValue == false || mind.Session == null)
                        {
                            // For now assume this means soul departed
                            // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-soul-broken"));
                            return;
                        }
                        var profile = GetPlayerProfileAsync(mindUser.Value);

                            cloningPodSystem.TryCloning(mind, profile, consoleComponent.CloningPod);
                    }
                }
        }
        public CloningConsoleBoundUserInterfaceState? GetUserInterfaceState(CloningConsoleComponent consoleComponent)
        {
            FindScanner(consoleComponent.Owner, consoleComponent);

            // !TryComp<DamageableComponent>(scanBody.Value, out var damageable)
            EntityUid? scanBody = null;
            var scanBodyInfo = "Unknown";
            var scannerBodyIsAlive = false;
            bool scannerConnected = false;
            if (consoleComponent.GeneticScanner != null) {
                scannerConnected = true;
                scanBody = consoleComponent.GeneticScanner.BodyContainer.ContainedEntity;
                if (TryComp<MobStateComponent>(scanBody, out MobStateComponent? mobState))
                {
                    scannerBodyIsAlive = mobState.IsAlive();
                }
                if (TryComp<MetaDataComponent>(scanBody, out var scanMetaData))
                {
                    scanBodyInfo = scanMetaData.EntityName;
                }
            }

            EntityUid? cloneBody = null;
            var cloneBodyInfo = "Unknown";
            float cloningProgress = 0;
            float cloningTime = 30f;
            bool clonerProgressing = false;
            bool clonerConnected = false;
            bool clonerMindPresent = false;
            if (consoleComponent.CloningPod != null)
            {
                clonerConnected = true;
                cloneBody = consoleComponent.CloningPod.BodyContainer.ContainedEntity;
                if (TryComp<MetaDataComponent>(cloneBody, out var cloneMetaData))
                {
                    cloneBodyInfo = cloneMetaData.EntityName;
                }
                cloningProgress = consoleComponent.CloningPod.CloningProgress;
                cloningTime = consoleComponent.CloningPod.CloningProgress;
                clonerProgressing = consoleComponent.CloningPod.UiKnownPowerState && (consoleComponent.CloningPod.BodyContainer.ContainedEntity != null);
                clonerMindPresent =consoleComponent.CloningPod.Status == CloningPodStatus.Cloning;
            }

            // CHECK IF ALIVE/CLONING IN PROGRESS/ IS CLONING RETURN INFO
            // var cloningSystem = EntitySystem.Get<CloningSystem>();
            bool readyToClone = false;
            if (scannerConnected && clonerConnected && !clonerProgressing && scanBody != null && cloneBody == null)
            {
                readyToClone = true;
                // if no one is being cloned and someone has the potential to be cloned, find out blockers
            }

            bool scannerIsAlive = scannerBodyIsAlive;
            string? scannerBodyInfo = scanBodyInfo;
            string? cloningBodyInfo = cloneBodyInfo;
            List<string> cloneHistory = consoleComponent.CloningHistory;
            TimeSpan referenceTime = _gameTiming.CurTime;
            float progress = cloningProgress;
            float maximum = cloningTime;
            bool progressing = clonerProgressing;
            bool mindPresent = clonerMindPresent;
            bool ReadyToClone = readyToClone;
            bool ScannerConnected = scannerConnected;
            bool ClonerConnected = clonerConnected;


            return new CloningConsoleBoundUserInterfaceState(
                scannerIsAlive,
                scannerBodyInfo,
                cloningBodyInfo,
                cloneHistory,
                referenceTime,
                progress,
                maximum,
                progressing,
                mindPresent,
                ReadyToClone,
                ScannerConnected,
                ClonerConnected
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

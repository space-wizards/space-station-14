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

        public GeneticScannerComponent? FindScanner(EntityUid Owner, CloningConsoleComponent? cloneConsoleComp = null)
        {
            if (!Resolve(Owner, ref cloneConsoleComp))
                return null;

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
            return null;
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

        }

        public void TryClone(EntityUid uid, CloningConsoleComponent consoleComponent)
        {
             if (consoleComponent.GeneticScanner != null && consoleComponent.GeneticScanner._bodyContainer.ContainedEntity != null)
                {
                    var cloningPodSystem = EntitySystem.Get<CloningPodSystem>();
                    if (!TryComp<MindComponent>( consoleComponent.GeneticScanner._bodyContainer.ContainedEntity.Value, out MindComponent? mindComp) || mindComp.Mind == null)
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

            // CloningConsoleBoundUserInterfaceState EmptyUIState =
            // new(
            //     null,
            //     null,
            //     null,
            //     null,
            //     null,
            //     null,
            //     null,
            //     null,
            //     null
            //     );

            FindScanner(consoleComponent.Owner, consoleComponent);

            if (consoleComponent.GeneticScanner == null) {
                return null;
            }
            if (consoleComponent.CloningPod == null)
            {
                return null;
            }

            var scanBody = consoleComponent.GeneticScanner._bodyContainer.ContainedEntity;
            if (scanBody == null)
            {
                return null;
            }

            var cloneBody = consoleComponent.CloningPod.BodyContainer.ContainedEntity;
            if (scanBody == null)
            {
                return null;
            }
            if (!TryComp<DamageableComponent>(scanBody.Value, out var damageable))
            {
                return null;
            }

            var isAlive = false;
            if (TryComp<MobStateComponent>(scanBody, out MobStateComponent? mobState))
            {
                isAlive = mobState.IsAlive();
            }

            var scanBodyInfo = "Unknown";
            if (TryComp<MetaDataComponent>(scanBody, out var scanMetaData))
            {
                scanBodyInfo = scanMetaData.EntityName;
            }

            var cloneBodyInfo = "Unknown";
            if (TryComp<MetaDataComponent>(scanBody, out var cloneMetaData))
            {
                scanBodyInfo = cloneMetaData.EntityName;
            }

            // CHECK IF ALIVE/CLONING IN PROGRESS/ IS CLONING RETURN INFO
            var cloningSystem = EntitySystem.Get<CloningSystem>();

                // MindIdName = mindIdName;
                // ReferenceTime = refTime;
                // Progress = progress;
                // Maximum = maximum;
                // Progressing = progressing;
                // MindPresent = mindPresent;

            bool? scannerIsAlive = isAlive;
            string? scannerBodyInfo = scanBodyInfo;
            string? cloningBodyInfo = cloneBodyInfo;
            List<string> cloneHistory = consoleComponent.CloningHistory;
            Logger.Debug(_gameTiming.CurFrame.ToString());
            TimeSpan referenceTime = _gameTiming.CurTime;
            float progress = consoleComponent.CloningPod.CloningProgress;
            float maximum = consoleComponent.CloningPod.CloningTime;
            bool progressing = consoleComponent.CloningPod.UiKnownPowerState && (consoleComponent.CloningPod.BodyContainer.ContainedEntity != null);
            bool mindPresent = consoleComponent.CloningPod.Status == CloningPodStatus.Cloning;

            // var scanned = TryComp<MindComponent>(body.Value, out var mindComponent) &&
            //              mindComponent.Mind != null &&
            //              cloningSystem.HasDnaScan(mindComponent.Mind);

            return new CloningConsoleBoundUserInterfaceState(
                scannerIsAlive,
                scannerBodyInfo,
                cloningBodyInfo,
                cloneHistory,
                referenceTime,
                progress,
                maximum,
                progressing,
                mindPresent
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

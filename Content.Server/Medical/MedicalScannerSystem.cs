using Content.Server.Climbing;
using Content.Server.Cloning;
using Content.Server.Medical.Components;
using Content.Server.Power.Components;
using Content.Shared.Destructible;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Content.Server.MachineLinking.System;
using Content.Server.MachineLinking.Events;
using Content.Server.Cloning.Components;
using Content.Server.Construction;
using Content.Server.MobState;
using Robust.Server.Containers;

using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent; /// Hmm...

namespace Content.Server.Medical
{
    public sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly ClimbSystem _climbSystem = default!;
        [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;

        private const float UpdateRate = 1f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MedicalScannerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<MedicalScannerComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<MedicalScannerComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<MedicalScannerComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<MedicalScannerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<MedicalScannerComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<MedicalScannerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        }

        private void OnComponentInit(EntityUid uid, MedicalScannerComponent scannerComponent, ComponentInit args)
        {
            base.Initialize();
            scannerComponent.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"scanner-bodyContainer");
            _signalSystem.EnsureReceiverPorts(uid, MedicalScannerComponent.ScannerPort);
        }

        private void OnRelayMovement(EntityUid uid, MedicalScannerComponent scannerComponent, ref ContainerRelayMovementEntityEvent args)
        {
            if (!_blocker.CanInteract(args.Entity, scannerComponent.Owner))
                return;

            EjectBody(uid, scannerComponent);
        }

        private void AddInsertOtherVerb(EntityUid uid, MedicalScannerComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                IsOccupied(component) ||
                !component.CanInsert(args.Using.Value))
                return;

            string name = "Unknown";
            if (TryComp<MetaDataComponent>(args.Using.Value, out var metadata))
                name = metadata.EntityName;

            InteractionVerb verb = new()
            {
                Act = () => InsertBody(component.Owner, args.Target, component),
                Category = VerbCategory.Insert,
                Text = name
            };
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, MedicalScannerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Eject verb
            if (IsOccupied(component))
            {
                AlternativeVerb verb = new();
                verb.Act = () => EjectBody(uid, component);
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("medical-scanner-verb-noun-occupant");
                verb.Priority = 1; // Promote to top to make ejecting the ALT-click action
                args.Verbs.Add(verb);
            }

            // Self-insert verb
            if (!IsOccupied(component) &&
                component.CanInsert(args.User) &&
                _blocker.CanMove(args.User))
            {
                AlternativeVerb verb = new();
                verb.Act = () => InsertBody(component.Owner, args.User, component);
                verb.Text = Loc.GetString("medical-scanner-verb-enter");
                args.Verbs.Add(verb);
            }
        }

        private void OnDestroyed(EntityUid uid, MedicalScannerComponent scannerComponent, DestructionEventArgs args)
        {
            EjectBody(uid, scannerComponent);
        }

        private void HandleDragDropOn(EntityUid uid, MedicalScannerComponent scannerComponent, DragDropEvent args)
        {
            InsertBody(uid, args.Dragged, scannerComponent);
        }

        private void OnPortDisconnected(EntityUid uid, MedicalScannerComponent component, PortDisconnectedEvent args)
        {
            component.ConnectedConsole = null;
        }

        private void OnAnchorChanged(EntityUid uid, MedicalScannerComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(component.ConnectedConsole, out var console))
                return;

            if (args.Anchored)
            {
                _cloningConsoleSystem.RecheckConnections(component.ConnectedConsole.Value, console.CloningPod, uid, console);
                return;
            }
            _cloningConsoleSystem.UpdateUserInterface(console);
        }
        private MedicalScannerStatus GetStatus(MedicalScannerComponent scannerComponent)
        {
            if (TryComp<ApcPowerReceiverComponent>(scannerComponent.Owner, out var power) && power.Powered)
            {
                var body = scannerComponent.BodyContainer.ContainedEntity;
                if (body == null)
                    return MedicalScannerStatus.Open;

                if (!TryComp<MobStateComponent>(body.Value, out var state))
                {
                    return MedicalScannerStatus.Open;
                }

                return GetStatusFromDamageState(body.Value, state);
            }
            return MedicalScannerStatus.Off;
        }

        public bool IsOccupied(MedicalScannerComponent scannerComponent)
        {
            return scannerComponent.BodyContainer.ContainedEntity != null;
        }

        private MedicalScannerStatus GetStatusFromDamageState(EntityUid uid, MobStateComponent state)
        {
            if (_mobStateSystem.IsAlive(uid, state))
                return MedicalScannerStatus.Green;

            if (_mobStateSystem.IsCritical(uid, state))
                return MedicalScannerStatus.Red;

            if (_mobStateSystem.IsDead(uid, state))
                return MedicalScannerStatus.Death;

            return MedicalScannerStatus.Yellow;
        }

        private void UpdateAppearance(EntityUid uid, MedicalScannerComponent scannerComponent)
        {
            if (TryComp<AppearanceComponent>(scannerComponent.Owner, out var appearance))
            {
                appearance.SetData(MedicalScannerVisuals.Status, GetStatus(scannerComponent));
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;

            _updateDif -= UpdateRate;

            foreach (var scanner in EntityQuery<MedicalScannerComponent>())
            {
                UpdateAppearance(scanner.Owner, scanner);
            }
        }

        public void InsertBody(EntityUid uid, EntityUid user, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (scannerComponent.BodyContainer.ContainedEntity != null)
                return;

            if (!TryComp<MobStateComponent>(user, out var comp))
                return;

            scannerComponent.BodyContainer.Insert(user);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        public void EjectBody(EntityUid uid, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (scannerComponent.BodyContainer.ContainedEntity is not {Valid: true} contained) return;

            scannerComponent.BodyContainer.Remove(contained);
            _climbSystem.ForciblySetClimbing(contained, uid);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        private void OnRefreshParts(EntityUid uid, MedicalScannerComponent component, RefreshPartsEvent args)
        {
            var ratingFail = args.PartRatings[component.MachinePartCloningFailChance];

            component.CloningFailChanceMultiplier = MathF.Pow(component.PartRatingFailMultiplier, ratingFail - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, MedicalScannerComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("medical-scanner-upgrade-cloning", component.CloningFailChanceMultiplier);
        }
    }
}

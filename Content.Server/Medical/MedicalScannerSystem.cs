using Content.Server.Climbing;
using Content.Server.Medical.Components;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.DragDrop;
using Content.Shared.MobState.Components;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Server.Cloning.Components;

using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;

namespace Content.Server.Medical
{
    public sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly ClimbSystem _climbSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float UpdateRate = 1f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MedicalScannerComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<MedicalScannerComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<MedicalScannerComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<MedicalScannerComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<MedicalScannerComponent, ComponentRemove>(OnComponentRemove);
        }

        private void OnComponentInit(EntityUid uid, MedicalScannerComponent scannerComponent, ComponentInit args)
        {
            base.Initialize();
            scannerComponent.BodyContainer = scannerComponent.Owner.EnsureContainer<ContainerSlot>($"{scannerComponent.Name}-bodyContainer");
            TryMachineSync(uid, scannerComponent);
        }

        private void OnRelayMovement(EntityUid uid, MedicalScannerComponent scannerComponent, RelayMovementEntityEvent args)
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

        private void OnAnchorChange(EntityUid uid, MedicalScannerComponent scannerComponent, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                TryMachineSync(uid, scannerComponent);
            else
                DisconnectMachineConnections(uid, scannerComponent);
        }

        private void OnComponentRemove(EntityUid uid, MedicalScannerComponent scannerComponent, ComponentRemove args)
        {
            DisconnectMachineConnections(uid, scannerComponent);
        }

        private MedicalScannerStatus GetStatus(MedicalScannerComponent scannerComponent)
        {
            if (IsPowered(scannerComponent))
            {
                var body = scannerComponent.BodyContainer.ContainedEntity;
                if (body == null)
                    return MedicalScannerStatus.Open;

                if (!TryComp<MobStateComponent>(body.Value, out var state))
                {
                    return MedicalScannerStatus.Open;
                }

                return GetStatusFromDamageState(state);
            }
            return MedicalScannerStatus.Off;
        }

        public bool IsPowered(MedicalScannerComponent scannerComponent)
        {
            if (TryComp<ApcPowerReceiverComponent>(scannerComponent.Owner, out var receiver))
            {
                return receiver.Powered;
            }
            return false;
        }

        public bool IsOccupied(MedicalScannerComponent scannerComponent)
        {
            return scannerComponent.BodyContainer.ContainedEntity != null;
        }

        private MedicalScannerStatus GetStatusFromDamageState(MobStateComponent state)
        {
            if (state.IsAlive())
                return MedicalScannerStatus.Green;

            if (state.IsCritical())
                return MedicalScannerStatus.Red;

            if (state.IsDead())
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
            _climbSystem.ForciblySetClimbing(contained);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        public void TryMachineSync(EntityUid uid, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (TryComp<TransformComponent>(uid, out var transformComp) && transformComp.Anchored)
            {
                var grid = _mapManager.GetGrid(transformComp.GridID);
                var coords = transformComp.Coordinates;
                foreach (var entity in grid.GetCardinalNeighborCells(coords))
                {
                    if (TryComp<CloningConsoleComponent>(entity, out var cloningConsole))
                    {
                        if (cloningConsole.GeneticScanner == null)
                        {
                            cloningConsole.GeneticScanner = uid;
                            scannerComponent.ConnectedConsole = entity;
                        }
                        break;
                    }
                }
            }
        }

        public void DisconnectMachineConnections(EntityUid uid, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (scannerComponent.ConnectedConsole == null)
                return;

            if (TryComp<CloningConsoleComponent>(scannerComponent.ConnectedConsole, out var cloningConsole) && cloningConsole.GeneticScanner == uid)
                cloningConsole.GeneticScanner = null;

            scannerComponent.ConnectedConsole = null;
        }
    }
}

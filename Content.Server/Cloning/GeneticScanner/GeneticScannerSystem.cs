using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.Climbing;
using Content.Shared.MobState.Components;
using Content.Shared.DragDrop;
using Content.Shared.Acts;
using Content.Server.Power.Components;
using Robust.Shared.Containers;
using Content.Shared.Cloning.GeneticScanner;

using static Content.Shared.Cloning.GeneticScanner.SharedGeneticScannerComponent;

namespace Content.Server.Cloning.GeneticScanner
{
    [UsedImplicitly]
    internal sealed class GeneticScannerSystem : SharedGeneticScannerSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GeneticScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<GeneticScannerComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<GeneticScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<GeneticScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<GeneticScannerComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<GeneticScannerComponent, DragDropEvent>(HandleDragDropOn);
        }

        private void OnComponentInit(EntityUid uid, GeneticScannerComponent scannerComponent, ComponentInit args)
        {
            scannerComponent.BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(scannerComponent.Owner, $"{Name}-bodyContainer");
        }

        private void AddInsertOtherVerb(EntityUid uid, GeneticScannerComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                IsOccupied(component) ||
                !component.CanInsert(args.Using.Value))
                return;

            InteractionVerb verb = new();
            verb.Act = () => InsertBody(uid, component);
            verb.Category = VerbCategory.Insert;
            verb.Text = EntityManager.GetComponent<MetaDataComponent>(args.Using.Value).EntityName;
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, GeneticScannerComponent component, GetVerbsEvent<AlternativeVerb> args)
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

            if (component.BodyContainer == null) {
                return;
            }

            // Self-insert verb
            if (!IsOccupied(component) &&
                component.CanInsert(args.User) &&
                _actionBlockerSystem.CanMove(args.User))
            {
                AlternativeVerb verb = new();
                verb.Act = () => InsertBody(args.User, component);
                verb.Text = Loc.GetString("medical-scanner-verb-enter");
                args.Verbs.Add(verb);
            }
        }

        private void OnRelayMovement(EntityUid uid, GeneticScannerComponent scannerComponent, RelayMovementEntityEvent args)
        {
            if (_blocker.CanInteract(args.Entity, scannerComponent.Owner))
            {
                if (_gameTiming.CurTime <
                    scannerComponent.LastInternalOpenAttempt + GeneticScannerComponent.InternalOpenAttemptDelay)
                {
                    return;
                }

                scannerComponent.LastInternalOpenAttempt = _gameTiming.CurTime;
                EjectBody(uid, scannerComponent);
            }
        }

        private void OnDestroyed(EntityUid uid, GeneticScannerComponent scannerComponent, DestructionEventArgs args)
        {
            EjectBody(uid, scannerComponent);
        }

        private void HandleDragDropOn(EntityUid uid, GeneticScannerComponent scannerComponent, DragDropEvent args)
        {
            scannerComponent.BodyContainer.Insert(args.Dragged);
        }

        private GeneticScannerStatus GetStatus(GeneticScannerComponent scannerComponent)
        {
            if (IsPowered(scannerComponent))
            {
                var body = scannerComponent.BodyContainer.ContainedEntity;
                if (body == null)
                    return GeneticScannerStatus.Open;

                if (!TryComp<MobStateComponent>(body.Value, out var state))
                {
                    return GeneticScannerStatus.Open;
                }

                return GetStatusFromDamageState(state);
            }
            return GeneticScannerStatus.Off;
        }

        public bool IsPowered(GeneticScannerComponent scannerComponent)
        {
            if (TryComp<ApcPowerReceiverComponent>(scannerComponent.Owner, out var receiver))
            {
                return receiver.Powered;
            }
            return false;
        }

        public bool IsOccupied(GeneticScannerComponent scannerComponent)
        {
            if (scannerComponent.BodyContainer != null)
            {
                return scannerComponent.BodyContainer.ContainedEntity != null;
            }
            return false;
        }

        private GeneticScannerStatus GetStatusFromDamageState(MobStateComponent state)
        {
            if (state.IsAlive())
            {
                return GeneticScannerStatus.Green;
            }
            else if (state.IsCritical())
            {
                return GeneticScannerStatus.Red;
            }
            else if (state.IsDead())
            {
                return GeneticScannerStatus.Death;
            }
            else
            {
                return GeneticScannerStatus.Yellow;
            }
        }

        private void UpdateAppearance(EntityUid uid, GeneticScannerComponent scannerComponent)
        {
            if (TryComp<AppearanceComponent>(scannerComponent.Owner, out var appearance))
            {
                appearance.SetData(GeneticScannerVisuals.Status, GetStatus(scannerComponent));
            }
        }

        public void InsertBody(EntityUid user, GeneticScannerComponent? scannerComponent)
        {
            if (!Resolve(user, ref scannerComponent))
                return;

            if (scannerComponent.BodyContainer == null || scannerComponent.BodyContainer.ContainedEntity != null)
                return;

            if (!TryComp<MobStateComponent>(user, out MobStateComponent? comp))
                return;

            scannerComponent.BodyContainer.Insert(user);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        public void EjectBody(EntityUid uid, GeneticScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (scannerComponent.BodyContainer.ContainedEntity is not {Valid: true} contained) return;

            scannerComponent.BodyContainer.Remove(contained);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
            EntitySystem.Get<ClimbSystem>().ForciblySetClimbing(contained);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<GeneticScannerComponent>())
            {
                UpdateAppearance(comp.Owner, comp);
            }
        }
    }
}

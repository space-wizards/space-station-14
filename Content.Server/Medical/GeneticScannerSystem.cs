using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Content.Shared.GeneticScanner;
using Content.Server.Climbing;
using Content.Shared.MobState.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Log;
using Content.Shared.Acts;
using Content.Server.Power.Components;


using Content.Server.Preferences.Managers;


using static Content.Shared.GeneticScanner.SharedGeneticScannerComponent;

namespace Content.Server.Medical.GeneticScanner
{
    [UsedImplicitly]
    internal sealed class GeneticScannerSystem : SharedGeneticScannerSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GeneticScannerComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<GeneticScannerComponent, GetInteractionVerbsEvent>(AddInsertOtherVerb);
            SubscribeLocalEvent<GeneticScannerComponent, GetAlternativeVerbsEvent>(AddAlternativeVerbs);
            SubscribeLocalEvent<GeneticScannerComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<GeneticScannerComponent, DragDropEvent>(HandleDragDropOn);
        }

        private void AddInsertOtherVerb(EntityUid uid, GeneticScannerComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.IsOccupied ||
                !component.CanInsert(args.Using.Value))
                return;

            Verb verb = new();
            verb.Act = () => InsertBody(uid, component);
            verb.Category = VerbCategory.Insert;
            verb.Text = EntityManager.GetComponent<MetaDataComponent>(args.Using.Value).EntityName;
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, GeneticScannerComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Eject verb
            if (component.IsOccupied)
            {
                Verb verb = new();
                verb.Act = () => EjectBody(uid, component);
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("medical-scanner-verb-noun-occupant");
                args.Verbs.Add(verb);
            }

            if (component._bodyContainer == null) {
                Logger.Debug("CONTAINER NULL");
            }

            // Self-insert verb
            if (!component.IsOccupied &&
                component.CanInsert(args.User) &&
                _actionBlockerSystem.CanMove(args.User))
            {
                Verb verb = new();
                verb.Act = () => InsertBody(args.User, component);
                verb.Text = Loc.GetString("medical-scanner-verb-enter");
                args.Verbs.Add(verb);
            }
        }

        private void OnRelayMovement(EntityUid uid, GeneticScannerComponent scannerComponent, RelayMovementEntityEvent args)
        {
            if (_blocker.CanInteract(args.Entity))
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
            scannerComponent._bodyContainer.Insert(args.Dragged);
        }

        public GeneticScannerStatus GetStatus(GeneticScannerComponent scannerComponent)
        {
            if (IsPowered(scannerComponent))
            {
                var body = scannerComponent._bodyContainer.ContainedEntity;
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

        public void InsertBody(EntityUid user, GeneticScannerComponent scannerComponent)
        {
            // if (TryComp<GeneticScannerEntityStorageComponent>(scannerComponent.Owner, out var storage))
            scannerComponent._bodyContainer.Insert(user);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        public void EjectBody(EntityUid uid, GeneticScannerComponent scannerComponent)
        {
            if (scannerComponent._bodyContainer.ContainedEntity is not {Valid: true} contained) return;
            scannerComponent._bodyContainer.Remove(contained);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
            EntitySystem.Get<ClimbSystem>().ForciblySetClimbing(contained);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<GeneticScannerComponent>())
            {
                // comp.Update(frameTime);
                UpdateAppearance(comp.Owner, comp);
            }
        }
    }
}

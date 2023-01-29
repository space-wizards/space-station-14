using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;

namespace Content.Shared.Cuffs
{
    public abstract class SharedCuffableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedCuffableComponent, EntRemovedFromContainerMessage>(OnCuffCountChanged);
            SubscribeLocalEvent<SharedCuffableComponent, EntInsertedIntoContainerMessage>(OnCuffCountChanged);
            SubscribeLocalEvent<SharedCuffableComponent, RejuvenateEvent>(OnRejuvenate);

            SubscribeLocalEvent<SharedCuffableComponent, StopPullingEvent>(HandleStopPull);
            SubscribeLocalEvent<SharedCuffableComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, AttackAttemptEvent>(CheckAct);
            SubscribeLocalEvent<SharedCuffableComponent, UseAttemptEvent>(CheckAct);
            SubscribeLocalEvent<SharedCuffableComponent, InteractionAttemptEvent>(CheckAct);
            SubscribeLocalEvent<SharedCuffableComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, DropAttemptEvent>(CheckAct);
            SubscribeLocalEvent<SharedCuffableComponent, PickupAttemptEvent>(CheckAct);
            SubscribeLocalEvent<SharedCuffableComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, PullStartedMessage>(OnPull);
            SubscribeLocalEvent<SharedCuffableComponent, PullStoppedMessage>(OnPull);
        }

        private void OnRejuvenate(EntityUid uid, SharedCuffableComponent component, RejuvenateEvent args)
        {
            _container.EmptyContainer(component.Container, true, attachToGridOrMap: true);
        }

        private void OnCuffCountChanged(EntityUid uid, SharedCuffableComponent component, ContainerModifiedMessage args)
        {
            if (args.Container == component.Container)
                UpdateCuffState(uid, component);
        }

        public void UpdateCuffState(EntityUid uid, SharedCuffableComponent component)
        {
            var canInteract = TryComp(uid, out SharedHandsComponent? hands) && hands.Hands.Count > component.CuffedHandCount;

            if (canInteract == component.CanStillInteract)
                return;

            component.CanStillInteract = canInteract;
            Dirty(component);
            _blocker.UpdateCanMove(uid);

            if (component.CanStillInteract)
                _alerts.ClearAlert(uid, AlertType.Handcuffed);
            else
                _alerts.ShowAlert(uid, AlertType.Handcuffed);

            var ev = new CuffedStateChangeEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        private void OnBeingPulledAttempt(EntityUid uid, SharedCuffableComponent component, BeingPulledAttemptEvent args)
        {
            if (!TryComp<SharedPullableComponent>(uid, out var pullable))
                return;

            if (pullable.Puller != null && !component.CanStillInteract) // If we are being pulled already and cuffed, we can't get pulled again.
                args.Cancel();
        }
        private void OnPull(EntityUid uid, SharedCuffableComponent component, PullMessage args)
        {
            if (!component.CanStillInteract)
                _blocker.UpdateCanMove(uid);
        }

        private void HandleMoveAttempt(EntityUid uid, SharedCuffableComponent component, UpdateCanMoveEvent args)
        {
            if (component.CanStillInteract || !EntityManager.TryGetComponent(uid, out SharedPullableComponent? pullable) || !pullable.BeingPulled)
                return;

            args.Cancel();
        }

        private void HandleStopPull(EntityUid uid, SharedCuffableComponent component, StopPullingEvent args)
        {
            if (args.User == null || !EntityManager.EntityExists(args.User.Value)) return;

            if (args.User.Value == component.Owner && !component.CanStillInteract)
            {
                args.Cancel();
            }
        }

        #region ActionBlocker

        private void CheckAct(EntityUid uid, SharedCuffableComponent component, CancellableEntityEventArgs args)
        {
            if (!component.CanStillInteract)
                args.Cancel();
        }

        private void OnEquipAttempt(EntityUid uid, SharedCuffableComponent component, IsEquippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Equipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnUnequipAttempt(EntityUid uid, SharedCuffableComponent component, IsUnequippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Unequipee == uid)
                CheckAct(uid, component, args);
        }

        #endregion
    }
}

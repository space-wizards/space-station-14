using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs.Components;
using Content.Shared.DragDrop;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;

namespace Content.Shared.Cuffs
{
    public abstract class SharedCuffableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedCuffableComponent, StopPullingEvent>(HandleStopPull);
            SubscribeLocalEvent<SharedCuffableComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, PullStartedMessage>(OnPull);
            SubscribeLocalEvent<SharedCuffableComponent, PullStoppedMessage>(OnPull);
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

        private void OnUseAttempt(EntityUid uid, SharedCuffableComponent component, UseAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnInteractAttempt(EntityUid uid, SharedCuffableComponent component, InteractionAttemptEvent args)
        {
            CheckAct(uid, component, args);
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

        private void OnDropAttempt(EntityUid uid, SharedCuffableComponent component, DropAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnPickupAttempt(EntityUid uid, SharedCuffableComponent component, PickupAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        #endregion
    }
}

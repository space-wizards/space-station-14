using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands;
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
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CuffableComponent, EntRemovedFromContainerMessage>(OnCuffCountChanged);
            SubscribeLocalEvent<CuffableComponent, EntInsertedIntoContainerMessage>(OnCuffCountChanged);
            SubscribeLocalEvent<CuffableComponent, RejuvenateEvent>(OnRejuvenate);

            SubscribeLocalEvent<CuffableComponent, ComponentInit>(OnStartup);
            SubscribeLocalEvent<CuffableComponent, StopPullingEvent>(HandleStopPull);
            SubscribeLocalEvent<CuffableComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
            SubscribeLocalEvent<CuffableComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<CuffableComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<CuffableComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
            SubscribeLocalEvent<CuffableComponent, PullStartedMessage>(OnPull);
            SubscribeLocalEvent<CuffableComponent, PullStoppedMessage>(OnPull);
            SubscribeLocalEvent<CuffableComponent, DropAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, PickupAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, AttackAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, UseAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, InteractionAttemptEvent>(CheckAct);

            SubscribeLocalEvent<HandCountChangedEvent>(OnHandCountChanged);
        }

        private void OnStartup(EntityUid uid, CuffableComponent component, ComponentInit args)
        {
            component.Container = _container.EnsureContainer<Container>(uid, _componentFactory.GetComponentName(component.GetType()));
        }

        private void OnRejuvenate(EntityUid uid, CuffableComponent component, RejuvenateEvent args)
        {
            _container.EmptyContainer(component.Container, true, attachToGridOrMap: true);
        }

        private void OnCuffCountChanged(EntityUid uid, CuffableComponent component, ContainerModifiedMessage args)
        {
            if (args.Container == component.Container)
                UpdateCuffState(uid, component);
        }

        public void UpdateCuffState(EntityUid uid, CuffableComponent component)
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

        private void OnBeingPulledAttempt(EntityUid uid, CuffableComponent component, BeingPulledAttemptEvent args)
        {
            if (!TryComp<SharedPullableComponent>(uid, out var pullable))
                return;

            if (pullable.Puller != null && !component.CanStillInteract) // If we are being pulled already and cuffed, we can't get pulled again.
                args.Cancel();
        }

        private void OnPull(EntityUid uid, CuffableComponent component, PullMessage args)
        {
            if (!component.CanStillInteract)
                _blocker.UpdateCanMove(uid);
        }

        private void HandleMoveAttempt(EntityUid uid, CuffableComponent component, UpdateCanMoveEvent args)
        {
            if (component.CanStillInteract || !EntityManager.TryGetComponent(uid, out SharedPullableComponent? pullable) || !pullable.BeingPulled)
                return;

            args.Cancel();
        }

        private void HandleStopPull(EntityUid uid, CuffableComponent component, StopPullingEvent args)
        {
            if (args.User == null || !Exists(args.User.Value))
                return;

            if (args.User.Value == uid && !component.CanStillInteract)
                args.Cancel();
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!TryComp(owner, out CuffableComponent? cuffable) ||
                !cuffable.Initialized)
            {
                return;
            }

            var dirty = false;
            var handCount = CompOrNull<SharedHandsComponent>(owner)?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                container.Remove(entity);
                _transform.SetWorldPosition(entity, _transform.GetWorldPosition(owner));
            }

            if (dirty)
            {
                UpdateCuffState(owner, cuffable);
            }
        }

        #region ActionBlocker

        private void CheckAct(EntityUid uid, CuffableComponent component, CancellableEntityEventArgs args)
        {
            if (!component.CanStillInteract)
                args.Cancel();
        }

        private void OnEquipAttempt(EntityUid uid, CuffableComponent component, IsEquippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Equipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnUnequipAttempt(EntityUid uid, CuffableComponent component, IsUnequippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Unequipee == uid)
                CheckAct(uid, component, args);
        }

        #endregion
    }
}

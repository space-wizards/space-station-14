using Content.Shared.Cuffs.Components;
using Content.Shared.DragDrop;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement;
using Content.Shared.Pulling.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Cuffs
{
    public abstract class SharedCuffableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedCuffableComponent, StopPullingEvent>(HandleStopPull);
            SubscribeLocalEvent<SharedCuffableComponent, MovementAttemptEvent>(HandleMoveAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, EquipAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, UnequipAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<SharedCuffableComponent, PickupAttemptEvent>(OnPickupAttempt);
        }

        private void HandleMoveAttempt(EntityUid uid, SharedCuffableComponent component, MovementAttemptEvent args)
        {
            if (component.CanStillInteract || !EntityManager.TryGetComponent(uid, out SharedPullableComponent? pullable) || !pullable.BeingPulled)
                return;

            args.Cancel();
        }

        private void HandleStopPull(EntityUid uid, SharedCuffableComponent component, StopPullingEvent args)
        {
            if (args.User == null || !EntityManager.TryGetEntity(args.User.Value, out var user)) return;

            if (user == component.Owner && !component.CanStillInteract)
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

        private void OnEquipAttempt(EntityUid uid, SharedCuffableComponent component, EquipAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnUnequipAttempt(EntityUid uid, SharedCuffableComponent component, UnequipAttemptEvent args)
        {
            CheckAct(uid, component, args);
        }

        private void OnAttackAttempt(EntityUid uid, SharedCuffableComponent component, AttackAttemptEvent args)
        {
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

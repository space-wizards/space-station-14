using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;

namespace Content.Shared.Pulling.Systems
{
    [UsedImplicitly]
    public sealed class SharedPullerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPullingSystem _pullSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedPullerComponent, PullStartedMessage>(PullerHandlePullStarted);
            SubscribeLocalEvent<SharedPullerComponent, PullStoppedMessage>(PullerHandlePullStopped);
            SubscribeLocalEvent<SharedPullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<SharedPullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnVirtualItemDeleted(EntityUid uid, SharedPullerComponent component, VirtualItemDeletedEvent args)
        {
            if (component.Pulling == null)
                return;

            if (component.Pulling == args.BlockingEntity)
            {
                if (EntityManager.TryGetComponent<SharedPullableComponent>(args.BlockingEntity, out var comp))
                {
                    _pullSystem.TryStopPull(comp, uid);
                }
            }
        }

        private void PullerHandlePullStarted(
            EntityUid uid,
            SharedPullerComponent component,
            PullStartedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            _alertsSystem.ShowAlert(component.Owner, AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private void PullerHandlePullStopped(
            EntityUid uid,
            SharedPullerComponent component,
            PullStoppedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            var euid = component.Owner;
            _alertsSystem.ClearAlert(euid, AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private void OnRefreshMovespeed(EntityUid uid, SharedPullerComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }

        private void RefreshMovementSpeed(SharedPullerComponent component)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers((component).Owner);
        }
    }
}

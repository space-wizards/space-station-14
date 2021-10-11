using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Movement.Components;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Pulling.Systems
{
    [UsedImplicitly]
    public sealed class SharedPullerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPullingSystem _pullSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedPullerComponent, PullStartedMessage>(PullerHandlePullStarted);
            SubscribeLocalEvent<SharedPullerComponent, PullStoppedMessage>(PullerHandlePullStopped);
            SubscribeLocalEvent<SharedPullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        }

        private void OnVirtualItemDeleted(EntityUid uid, SharedPullerComponent component, VirtualItemDeletedEvent args)
        {
            if (component.Pulling == null)
                return;

            if (component.Pulling == EntityManager.GetEntity(args.BlockingEntity))
            {
                if (EntityManager.TryGetComponent<SharedPullableComponent>(args.BlockingEntity, out var comp))
                {
                    _pullSystem.TryStopPull(comp, EntityManager.GetEntity(uid));
                }
            }
        }

        private static void PullerHandlePullStarted(
            EntityUid uid,
            SharedPullerComponent component,
            PullStartedMessage args)
        {
            if (args.Puller.Owner.Uid != uid)
                return;

            if (component.Owner.TryGetComponent(out SharedAlertsComponent? alerts))
                alerts.ShowAlert(AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private static void PullerHandlePullStopped(
            EntityUid uid,
            SharedPullerComponent component,
            PullStoppedMessage args)
        {
            if (args.Puller.Owner.Uid != uid)
                return;

            if (component.Owner.TryGetComponent(out SharedAlertsComponent? alerts))
                alerts.ClearAlert(AlertType.Pulling);

            RefreshMovementSpeed(component);
        }

        private static void RefreshMovementSpeed(SharedPullerComponent component)
        {
            // Before changing how this is updated, please see SharedPullerComponent
            if (component.Owner.TryGetComponent<MovementSpeedModifierComponent>(out var speed))
            {
                speed.RefreshMovementSpeedModifiers();
            }
        }
    }
}

using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Pulling.Systems
{
    [UsedImplicitly]
    public sealed class SharedPullerSystem : EntitySystem
    {
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
                    comp.TryStopPull(EntityManager.GetEntity(uid));
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
        }
    }
}

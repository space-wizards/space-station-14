using Content.Shared.Alert;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Pulling
{
    [UsedImplicitly]
    public sealed class SharedPullerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedPullerComponent, PullStartedMessage>(PullerHandlePullStarted);
            SubscribeLocalEvent<SharedPullerComponent, PullStoppedMessage>(PullerHandlePullStopped);
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

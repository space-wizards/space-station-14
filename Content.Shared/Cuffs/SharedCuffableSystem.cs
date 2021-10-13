using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction.Events;
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

        private void OnUseAttempt(EntityUid uid, SharedCuffableComponent component, UseAttemptEvent args)
        {
            if (!component.CanStillInteract)
                args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, SharedCuffableComponent component, InteractionAttemptEvent args)
        {
            if (!component.CanStillInteract)
                args.Cancel();
        }
    }
}

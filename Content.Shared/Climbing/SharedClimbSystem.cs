using Content.Shared.Movement;

namespace Content.Shared.Climbing
{
    public abstract class SharedClimbSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<SharedClimbingComponent, MovementAttemptEvent>(OnMoveAttempt);
        }

        private void OnMoveAttempt(EntityUid uid, SharedClimbingComponent component, MovementAttemptEvent args)
        {
            if (component.OwnerIsTransitioning)
                args.Cancel();
        }
    }
}

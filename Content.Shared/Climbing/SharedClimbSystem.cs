using Content.Shared.Movement;
using Robust.Shared.GameObjects;

namespace Content.Shared.Climbing
{
    public abstract class SharedClimbSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedClimbingComponent, MovementAttemptEvent>(HandleMoveAttempt);
        }

        private void HandleMoveAttempt(EntityUid uid, SharedClimbingComponent component, MovementAttemptEvent args)
        {
            if (component.OwnerIsTransitioning)
                args.Cancel();
        }
    }
}

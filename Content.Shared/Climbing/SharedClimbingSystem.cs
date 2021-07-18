using Content.Shared.Movement;
using Robust.Shared.GameObjects;

namespace Content.Shared.Climbing
{
    public abstract class SharedClimbingSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedClimbingComponent, MovementAttemptEvent>(HandleMove);
        }

        private void HandleMove(EntityUid uid, SharedClimbingComponent component, MovementAttemptEvent args)
        {
            if (!component.GetIsTransitioning()) return;
            args.Cancel();
        }
    }
}

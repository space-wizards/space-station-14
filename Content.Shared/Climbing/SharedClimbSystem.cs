using Content.Shared.Movement;
using Robust.Shared.GameObjects;

namespace Content.Shared.Climbing
{
    public abstract class SharedClimbSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedClimbingComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
        }

        private void HandleMoveAttempt(EntityUid uid, SharedClimbingComponent component, UpdateCanMoveEvent args)
        {
            if (component.LifeStage > ComponentLifeStage.Running)
                return;

            if (component.OwnerIsTransitioning)
                args.Cancel();
        }
    }
}

using Content.Shared.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Stunnable
{
    [UsedImplicitly]
    internal sealed class StunSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedStunnableComponent, MovementAttemptEvent>(HandleMoveAttempt);
        }

        private void HandleMoveAttempt(EntityUid uid, SharedStunnableComponent component, MovementAttemptEvent args)
        {
            if (component.Stunned)
                args.Cancel();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in EntityManager.EntityQuery<SharedStunnableComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}

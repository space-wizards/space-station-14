using Content.Shared.Movement;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Shuttles
{
    public abstract class SharedShuttleConsoleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PilotComponent, MovementAttemptEvent>(HandleMovementBlock);
        }

        private void HandleMovementBlock(EntityUid uid, PilotComponent component, MovementAttemptEvent args)
        {
            if (component.Console == null) return;
            args.Cancel();
        }
    }
}

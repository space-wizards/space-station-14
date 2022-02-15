using Content.Shared.Movement;
using Content.Shared.Shuttles.Components;

namespace Content.Shared.Shuttles
{
    public abstract class SharedShuttleConsoleSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<PilotComponent, MovementAttemptEvent>(OnMovementAttempt);
        }

        private void OnMovementAttempt(EntityUid uid, PilotComponent component, MovementAttemptEvent args)
        {
            if (component.Console == null) return;
            args.Cancel();
        }
    }
}

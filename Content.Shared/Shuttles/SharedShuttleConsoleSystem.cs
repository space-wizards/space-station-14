using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Shuttles.Components;

namespace Content.Shared.Shuttles
{
    public abstract class SharedShuttleConsoleSystem : EntitySystem
    {
        [Dependency] protected readonly ActionBlockerSystem ActionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PilotComponent, UpdateCanMoveEvent>(HandleMovementBlock);
            SubscribeLocalEvent<PilotComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PilotComponent, ComponentShutdown>(HandlePilotShutdown);
        }

        protected virtual void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
        {
            ActionBlockerSystem.UpdateCanMove(uid);
        }

        private void OnStartup(EntityUid uid, PilotComponent component, ComponentStartup args)
        {
            ActionBlockerSystem.UpdateCanMove(uid);
        }

        private void HandleMovementBlock(EntityUid uid, PilotComponent component, UpdateCanMoveEvent args)
        {
            if (component.LifeStage > ComponentLifeStage.Running)
                return;

            if (component.Console == null) return;
            args.Cancel();
        }
    }
}

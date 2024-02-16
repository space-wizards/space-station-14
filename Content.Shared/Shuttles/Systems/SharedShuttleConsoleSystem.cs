using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems
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

        [Serializable, NetSerializable]
        protected sealed class PilotComponentState : ComponentState
        {
            public NetEntity? Console { get; }

            public PilotComponentState(NetEntity? uid)
            {
                Console = uid;
            }
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
            if (component.Console == null)
                return;

            args.Cancel();
        }
    }
}

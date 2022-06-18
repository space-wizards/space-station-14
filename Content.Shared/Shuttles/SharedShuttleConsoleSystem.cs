using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
            SubscribeLocalEvent<PilotComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<PilotComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, PilotComponent component, ref ComponentGetState args)
        {
            args.State = new PilotComponentState(component.Console?.Owner);
        }

        private void OnHandleState(EntityUid uid, PilotComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PilotComponentState state) return;

            var console = state.Console.GetValueOrDefault();
            if (!console.IsValid())
            {
                component.Console = null;
                return;
            }

            if (!TryComp<SharedShuttleConsoleComponent>(console, out var shuttleConsoleComponent))
            {
                Logger.Warning($"Unable to set Helmsman console to {console}");
                return;
            }

            component.Console = shuttleConsoleComponent;
            ActionBlockerSystem.UpdateCanMove(uid);
        }

        [Serializable, NetSerializable]
        private sealed class PilotComponentState : ComponentState
        {
            public EntityUid? Console { get; }

            public PilotComponentState(EntityUid? uid)
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

            if (component.Console == null) return;
            args.Cancel();
        }
    }
}

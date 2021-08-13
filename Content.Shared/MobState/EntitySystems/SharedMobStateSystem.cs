using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Content.Shared.Movement;
using Content.Shared.Pulling.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.EntitySystems
{
    public class SharedMobStateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedMobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
            SubscribeLocalEvent<SharedMobStateComponent, MovementAttemptEvent>(OnMoveAttempt);
        }

        private void OnStartPullAttempt(EntityUid uid, SharedMobStateComponent component, StartPullAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }

        private void OnMoveAttempt(EntityUid uid, SharedMobStateComponent component, MovementAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedCriticalMobState:
                case SharedDeadMobState:
                    args.Cancel();
                    return;
                default:
                    return;
            }
        }
    }
}

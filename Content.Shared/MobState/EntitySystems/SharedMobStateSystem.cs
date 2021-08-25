using Content.Shared.Damage;
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
            SubscribeLocalEvent<SharedMobStateComponent, DamageChangedEvent>(UpdateState);
            SubscribeLocalEvent<SharedMobStateComponent, MovementAttemptEvent>(OnMoveAttempt);
        }

        private void OnStartPullAttempt(EntityUid uid, SharedMobStateComponent component, StartPullAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }

        public static void UpdateState(EntityUid _, SharedMobStateComponent component, DamageChangedEvent args)
        {
            component.UpdateState(args.Damageable.TotalDamage);
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

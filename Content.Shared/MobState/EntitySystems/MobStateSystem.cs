using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Content.Shared.Movement;
using Content.Shared.Pulling.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.EntitySystems
{
    public class MobStateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
            SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(UpdateState);
            SubscribeLocalEvent<MobStateComponent, MovementAttemptEvent>(OnMoveAttempt);
        }

        private void OnStartPullAttempt(EntityUid uid, MobStateComponent component, StartPullAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }

        public void UpdateState(EntityUid _, MobStateComponent component, DamageChangedEvent args)
        {
            component.UpdateState(args.Damageable.TotalDamage);
        }
		
        private void OnMoveAttempt(EntityUid uid, MobStateComponent component, MovementAttemptEvent args)
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

using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Content.Shared.Movement;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.EntitySystems
{
    public class MobStateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MobStateComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<MobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
            SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(UpdateState);
            SubscribeLocalEvent<MobStateComponent, MovementAttemptEvent>(OnMoveAttempt);
            SubscribeLocalEvent<MobStateComponent, StandAttemptEvent>(OnStandAttempt);
            // Note that there's no check for Down attempts because if a mob's in crit or dead, they can be downed...
        }

        private void OnChangeDirectionAttempt(EntityUid uid, MobStateComponent component, ChangeDirectionAttemptEvent args)
        {
            switch (component.CurrentState)
            {
                case SharedDeadMobState:
                case SharedCriticalMobState:
                    args.Cancel();
                    break;
            }
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

        private void OnStandAttempt(EntityUid uid, MobStateComponent component, StandAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }

      /*  public class MobStateChangedEvent : EntityEventArgs
        {
            public IEntity Entity => Component.Owner;

            public readonly MobStateComponent Component { get; }

            public readonly MobState? OldMobState { get; }

            public readonly MobState CurrentMobState { get; }

            public MobStateChangedEvent(MobStateComponent component, )
    } */
    } 
}

using Content.Shared.Hands;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.State
{
    public abstract class SharedDeadMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Dead;

        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);
            var wake = entityManager.EnsureComponent<CollisionWakeComponent>(uid);
            wake.Enabled = true;
            var standingState = EntitySystem.Get<StandingStateSystem>();
            standingState.Down(uid);

            if (standingState.IsDown(uid) && entityManager.TryGetComponent(uid, out PhysicsComponent? physics))
            {
                physics.CanCollide = false;
            }

            if (entityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }
        }

        public override void ExitState(EntityUid uid, IEntityManager entityManager)
        {
            base.ExitState(uid, entityManager);
            if (entityManager.HasComponent<CollisionWakeComponent>(uid))
            {
                entityManager.RemoveComponent<CollisionWakeComponent>(uid);
            }

            var standingState = EntitySystem.Get<StandingStateSystem>();
            standingState.Stand(uid);

            if (!standingState.IsDown(uid) && entityManager.TryGetComponent(uid, out PhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }
    }
}

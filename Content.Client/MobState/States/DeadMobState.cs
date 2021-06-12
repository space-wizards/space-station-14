using Content.Client.Standing;
using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.MobState.States
{
    public class DeadMobState : SharedDeadMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);

            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                physics.CanCollide = false;
            }
        }

        public override void ExitState(IEntity entity)
        {
            base.ExitState(entity);

            EntitySystem.Get<StandingStateSystem>().Standing(entity);

            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }
    }
}

using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs.State
{
    public class DeadMobState : SharedDeadMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);

            if (entity.TryGetComponent(out PhysicsComponent physics))
            {
                physics.CanCollide = false;
            }
        }

        public override void ExitState(IEntity entity)
        {
            base.ExitState(entity);

            EntitySystem.Get<StandingStateSystem>().Standing(entity);

            if (entity.TryGetComponent(out PhysicsComponent physics))
            {
                physics.CanCollide = true;
            }
        }
    }
}

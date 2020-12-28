using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Physics;

namespace Content.Client.GameObjects.Components.Mobs.State
{
    public class DeadState : SharedDeadState
    {
        public override void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);

            if (entity.TryGetComponent(out PhysicsComponent physics))
            {
                physics.Enabled = false;
            }
        }

        public override void ExitState(IEntity entity)
        {
            EntitySystem.Get<StandingStateSystem>().Standing(entity);

            if (entity.TryGetComponent(out PhysicsComponent physics))
            {
                physics.Enabled = true;
            }
        }

        public override void UpdateState(IEntity entity) { }
    }
}

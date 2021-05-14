using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Mobs.State
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

            if (entity.TryGetComponent(out ServerAlertsComponent? status))
            {
                status.ShowAlert(AlertType.HumanDead);
            }

            if (entity.TryGetComponent(out StunnableComponent? stun))
            {
                stun.CancelAll();
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);

            if (entity.TryGetComponent(out IPhysBody? physics))
            {
                // remove mob from layers where they can be hit by ranged weapon
                // other layers will stay unchanged
                foreach (var fixture in physics.Fixtures)
                    fixture.CollisionLayer &= (int) ~CollisionGroup.WeaponMask;
            }
        }

        public override void ExitState(IEntity entity)
        {
            base.ExitState(entity);

            if (entity.TryGetComponent(out IPhysBody? physics))
            {
                // this will make mob penetred by ranged weapon again
                // it will force-rewrite possible weapon immunity
                // other layers will stay unchanged
                foreach (var fixture in physics.Fixtures)
                    fixture.CollisionLayer |= (int) CollisionGroup.WeaponMask;
            }
        }
    }
}

using Content.Shared.Hands;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.State
{
    public abstract class SharedDeadMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Dead;

        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);
            var wake = entity.EnsureComponent<CollisionWakeComponent>();
            wake.Enabled = true;
            var standingState = EntitySystem.Get<StandingStateSystem>();
            standingState.Down(entity);

            if (standingState.IsDown(entity) && entity.TryGetComponent(out PhysicsComponent? physics))
            {
                physics.CanCollide = false;
            }

            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }
        }

        public override void ExitState(IEntity entity)
        {
            base.ExitState(entity);
            if (entity.HasComponent<CollisionWakeComponent>())
            {
                entity.RemoveComponent<CollisionWakeComponent>();
            }

            var standingState = EntitySystem.Get<StandingStateSystem>();
            standingState.Stand(entity);

            if (!standingState.IsDown(entity) && entity.TryGetComponent(out PhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }

        public override bool CanInteract()
        {
            return false;
        }

        public override bool CanMove()
        {
            return false;
        }

        public override bool CanUse()
        {
            return false;
        }

        public override bool CanThrow()
        {
            return false;
        }

        public override bool CanSpeak()
        {
            return false;
        }

        public override bool CanDrop()
        {
            return false;
        }

        public override bool CanPickup()
        {
            return false;
        }

        public override bool CanEmote()
        {
            return false;
        }

        public override bool CanAttack()
        {
            return false;
        }

        public override bool CanEquip()
        {
            return false;
        }

        public override bool CanUnequip()
        {
            return false;
        }

        public override bool CanChangeDirection()
        {
            return false;
        }

        public bool CanShiver()
        {
            return false;
        }

        public bool CanSweat()
        {
            return false;
        }
    }
}

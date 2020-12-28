using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    public abstract class SharedMovementController : AetherController
    {
        [Dependency] protected readonly IEntityManager EntityManager = default!;

        public override void Update(float frameTime)
        {
            foreach (var (mover, physics, transform) in GetComponents<IMoverComponent, PhysicsComponent, ITransformComponent>())
            {
                UpdateKinematics(mover, physics, transform);
            }
        }

        protected void UpdateKinematics(IMoverComponent mover, PhysicsComponent physics, ITransformComponent transform)
        {
            if (physics.BodyType == BodyType.Static) return;

            var weightless = mover.Owner.IsWeightless();
            // TODO: Need to replace Status with damping

            if (weightless)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(transform, mover, physics);

                if (!touching)
                {
                    //physics.Status = BodyStatus.InAir;
                    transform.LocalRotation = physics.LinearVelocity.GetDir().ToAngle();
                    return;
                }
                else
                {
                    //physics.Status = BodyStatus.OnGround;
                }
            }

            // TODO: movement check.
            var (walkDir, sprintDir) = mover.VelocityDir;
            var combined = walkDir + sprintDir;
            if (combined.LengthSquared < 0.001 || !ActionBlockerSystem.CanMove(mover.Owner) && !weightless)
            {
                // TODO: StopMoving?
            }
            else
            {
                if (weightless)
                {
                    physics.ApplyLinearImpulse(combined * mover.CurrentPushSpeed);
                    transform.LocalRotation = physics.LinearVelocity.GetDir().ToAngle();
                    return;
                }

                var total = walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed;
                {
                    physics.ApplyLinearImpulse(total);
                }

                transform.LocalRotation = total.GetDir().ToAngle();

                HandleFootsteps(mover);
            }
        }

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover,
            PhysicsComponent collider)
        {
            foreach (var entity in EntityManager.GetEntitiesInRange(transform.Owner, mover.GrabRange, true))
            {
                if (entity == transform.Owner)
                {
                    continue; // Don't try to push off of yourself!
                }

                if (!entity.TryGetComponent<PhysicsComponent>(out var otherCollider) ||
                    !otherCollider.Enabled ||
                    (collider.CollisionMask & otherCollider.CollisionLayer) == 0)
                {
                    continue;
                }

                // Don't count pulled entities
                /*
                if (otherCollider.HasController<PullController>())
                {
                    continue;
                }
                */

                // TODO: Item check.
                var touching = ((collider.CollisionMask & otherCollider.CollisionLayer) != 0x0
                                || (otherCollider.CollisionMask & collider.CollisionLayer) != 0x0) // Ensure collision
                               && !entity.HasComponent<IItemComponent>(); // This can't be an item

                if (touching)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual void HandleFootsteps(IMoverComponent mover) {}
    }
}

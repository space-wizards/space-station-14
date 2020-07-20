using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    internal class ThrownItemComponent : ProjectileComponent, ICollideBehavior
    {
        public const float DefaultThrowTime = 0.25f;

        private bool _shouldCollide = true;
        private bool _shouldStop = false;

        public override string Name => "ThrownItem";
        public override uint? NetID => ContentNetIDs.THROWN_ITEM;

        /// <summary>
        ///     User who threw the item.
        /// </summary>
        public IEntity User;

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (!_shouldCollide) return;
            if (entity.TryGetComponent(out CollidableComponent collid)) 
            {
                if (!collid.Hard) // ignore non hard
                    return;

                _shouldStop = true; // hit something hard => stop after this collision
            }
            if (entity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Brute, 10, Owner, User);
            }

            // Stop colliding with mobs, this mimics not having enough velocity to do damage
            // after impacting the first object.
            // For realism this should actually be changed when the velocity of the object is less than a threshold.
            // This would allow ricochets off walls, and weird gravity effects from slowing the object.
            if (Owner.TryGetComponent(out CollidableComponent body) && body.PhysicsShapes.Count >= 1)
            {
                _shouldCollide = false;
            }
        }

        private void StopThrow()
        {
            if (Deleted)
            {
                return;
            }

            if (Owner.TryGetComponent(out CollidableComponent body) && body.PhysicsShapes.Count >= 1)
            {
                body.PhysicsShapes[0].CollisionMask &= (int) ~CollisionGroup.ThrownItem;

                var physics = Owner.GetComponent<PhysicsComponent>();
                physics.LinearVelocity = Vector2.Zero;
                physics.Status = BodyStatus.OnGround;
                body.Status = BodyStatus.OnGround;
                Owner.RemoveComponent<ThrownItemComponent>();
                EntitySystem.Get<InteractionSystem>().LandInteraction(User, Owner, Owner.Transform.GridPosition);
            }
        }

        void ICollideBehavior.PostCollide(int collideCount)
        {
            if (_shouldStop && collideCount > 0)
            {
                StopThrow();
            }
        }

        public void StartThrow(Vector2 initialImpulse)
        {
            var comp = Owner.GetComponent<PhysicsComponent>();
            comp.Status = BodyStatus.InAir;
            comp.Momentum = initialImpulse;
            StartStopTimer();
        }

        private void StartStopTimer()
        {
            Timer.Spawn((int) (DefaultThrowTime * 1000), MaybeStopThrow);
        }

        private void MaybeStopThrow()
        {
            if (Deleted)
            {
                return;
            }

            if (IoCManager.Resolve<IPhysicsManager>().IsWeightless(Owner.Transform.GridPosition))
            {
                StartStopTimer();
                return;
            }

            StopThrow();
        }
    }
}

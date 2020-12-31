using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.Damage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.Projectiles
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
        public IEntity User { get; set; }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (!_shouldCollide) return;
            if (entity.TryGetComponent(out PhysicsComponent collid))
            {
                if (!collid.Hard) // ignore non hard
                    return;

                _shouldStop = true; // hit something hard => stop after this collision

                // Raise an event.
                EntitySystem.Get<InteractionSystem>().ThrowCollideInteraction(User, Owner, entity, Owner.Transform.Coordinates);
            }
            if (entity.TryGetComponent(out IDamageableComponent damage))
            {
                damage.ChangeDamage(DamageType.Blunt, 10, false, Owner);
            }

            // Stop colliding with mobs, this mimics not having enough velocity to do damage
            // after impacting the first object.
            // For realism this should actually be changed when the velocity of the object is less than a threshold.
            // This would allow ricochets off walls, and weird gravity effects from slowing the object.
            if (Owner.TryGetComponent(out IPhysicsComponent body) && body.PhysicsShapes.Count >= 1)
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

            if (Owner.TryGetComponent(out IPhysicsComponent body) && body.PhysicsShapes.Count >= 1)
            {
                body.PhysicsShapes[0].CollisionMask &= (int) ~CollisionGroup.ThrownItem;

                if (body.TryGetController(out ThrownController controller))
                {
                    controller.LinearVelocity = Vector2.Zero;
                }

                body.Status = BodyStatus.OnGround;

                Owner.RemoveComponent<ThrownItemComponent>();
                EntitySystem.Get<InteractionSystem>().LandInteraction(User, Owner, Owner.Transform.Coordinates);
            }
        }

        void ICollideBehavior.PostCollide(int collideCount)
        {
            if (_shouldStop && collideCount > 0)
            {
                StopThrow();
            }
        }

        public void StartThrow(Vector2 direction, float speed)
        {
            var comp = Owner.GetComponent<IPhysicsComponent>();
            comp.Status = BodyStatus.InAir;

            var controller = comp.EnsureController<ThrownController>();
            controller.Push(direction, speed);

            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity("/Audio/Effects/toss.ogg", Owner);

            StartStopTimer();
        }

        private void StartStopTimer()
        {
            Owner.SpawnTimer((int) (DefaultThrowTime * 1000), MaybeStopThrow);
        }

        private void MaybeStopThrow()
        {
            if (Deleted)
            {
                return;
            }

            if (IoCManager.Resolve<IPhysicsManager>().IsWeightless(Owner.Transform.Coordinates))
            {
                StartStopTimer();
                return;
            }

            StopThrow();
        }
    }
}

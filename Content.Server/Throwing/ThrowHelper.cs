using System;
using Content.Server.Interaction;
using Content.Server.Items;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Throwing
{
    internal static class ThrowHelper
    {
        private const float ThrowAngularImpulse = 1.5f;

        /// <summary>
        /// The minimum amount of time an entity needs to be thrown before the timer can be run.
        /// Anything below this threshold never enters the air.
        /// </summary>
        private const float FlyTime = 0.15f;

        /// <summary>
        ///     Tries to throw the entity if it has a physics component, otherwise does nothing.
        /// </summary>
        /// <param name="entity">The entity being thrown.</param>
        /// <param name="direction">A vector pointing from the entity to its destination.</param>
        /// <param name="strength">How much the direction vector should be multiplied for velocity.</param>
        /// <param name="user"></param>
        /// <param name="pushbackRatio">The ratio of impulse applied to the thrower</param>
        internal static void TryThrow(this IEntity entity, Vector2 direction, float strength = 1.0f, IEntity? user = null, float pushbackRatio = 1.0f)
        {
            if (entity.Deleted ||
                direction == Vector2.Zero ||
                strength <= 0f ||
                !entity.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            if (physicsComponent.BodyType == BodyType.Static)
            {
                Logger.Warning("Tried to throw entity {entity} but can't throw static bodies!");
                return;
            }

            if (entity.HasComponent<MobStateComponent>())
            {
                Logger.Warning("Throwing not supported for mobs!");
                return;
            }

            var comp = entity.EnsureComponent<ThrownItemComponent>();
            if (entity.HasComponent<ItemComponent>())
            {
                comp.Thrower = user;
                // Give it a l'il spin.
                if (!entity.HasTag("NoSpinOnThrow"))
                {
                    physicsComponent.ApplyAngularImpulse(ThrowAngularImpulse);
                }
                else
                {
                    entity.Transform.LocalRotation = direction.ToWorldAngle() - Math.PI;
                }

                if (user != null)
                    EntitySystem.Get<InteractionSystem>().ThrownInteraction(user, entity);
            }

            var impulseVector = direction.Normalized * strength * physicsComponent.Mass;
            physicsComponent.ApplyLinearImpulse(impulseVector);

            // Estimate time to arrival so we can apply OnGround status and slow it much faster.
            var time = (direction / strength).Length;

            if (time < FlyTime)
            {
                physicsComponent.BodyStatus = BodyStatus.OnGround;
            }
            else
            {
                physicsComponent.BodyStatus = BodyStatus.InAir;

                Timer.Spawn(TimeSpan.FromSeconds(time - FlyTime), () =>
                {
                    if (physicsComponent.Deleted) return;
                    physicsComponent.BodyStatus = BodyStatus.OnGround;
                    EntitySystem.Get<ThrownItemSystem>().LandComponent(comp);
                });
            }

            // Give thrower an impulse in the other direction
            if (user != null && pushbackRatio > 0.0f && user.TryGetComponent(out IPhysBody? body))
            {
                var msg = new ThrowPushbackAttemptEvent();
                body.Owner.EntityManager.EventBus.RaiseLocalEvent(body.Owner.Uid, msg);

                if (!msg.Cancelled)
                {
                    body.ApplyLinearImpulse(-impulseVector * pushbackRatio);
                }
            }
        }
    }
}

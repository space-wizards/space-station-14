using System;
using Content.Server.Interaction;
using Content.Server.Items;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        /// <param name="pushbackRatio">The ratio of impulse applied to the thrower - defaults to 10 because otherwise it's not enough to properly recover from getting spaced</param>
        internal static void TryThrow(this EntityUid entity, Vector2 direction, float strength = 1.0f, EntityUid? user = null, float pushbackRatio = 10.0f)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.GetComponent<MetaDataComponent>(entity).EntityDeleted ||
                strength <= 0f ||
                !entities.TryGetComponent(entity, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            if (physicsComponent.BodyType == BodyType.Static)
            {
                Logger.Warning("Tried to throw entity {entity} but can't throw static bodies!");
                return;
            }

            if (entities.HasComponent<MobStateComponent>(entity))
            {
                Logger.Warning("Throwing not supported for mobs!");
                return;
            }

            var comp = entity.EnsureComponent<ThrownItemComponent>();
            if (entities.HasComponent<ItemComponent>(entity))
            {
                comp.Thrower = user;
                // Give it a l'il spin.
                if (!entity.HasTag("NoSpinOnThrow"))
                {
                    physicsComponent.ApplyAngularImpulse(ThrowAngularImpulse);
                }
                else if(direction != Vector2.Zero)
                {
                    entities.GetComponent<TransformComponent>(entity).LocalRotation = direction.ToWorldAngle() - Math.PI;
                }

                if (user != null)
                    EntitySystem.Get<InteractionSystem>().ThrownInteraction(user.Value, entity);
            }

            var impulseVector = direction.Normalized * strength * physicsComponent.Mass;
            physicsComponent.ApplyLinearImpulse(impulseVector);

            // Estimate time to arrival so we can apply OnGround status and slow it much faster.
            var time = (direction / strength).Length;

            if (time < FlyTime)
            {
                physicsComponent.BodyStatus = BodyStatus.OnGround;
                EntitySystem.Get<ThrownItemSystem>().LandComponent(comp);
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
            if (user != null && pushbackRatio > 0.0f && entities.TryGetComponent(user.Value, out IPhysBody? body))
            {
                var msg = new ThrowPushbackAttemptEvent();
                entities.EventBus.RaiseLocalEvent(body.Owner, msg);

                if (!msg.Cancelled)
                {
                    body.ApplyLinearImpulse(-impulseVector * pushbackRatio);
                }
            }
        }
    }
}

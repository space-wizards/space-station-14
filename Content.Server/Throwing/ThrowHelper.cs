#nullable enable
using Content.Server.Interaction;
using Content.Server.Items;
using Content.Shared.MobState;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Throwing
{
    internal static class ThrowHelper
    {
        private const float ThrowAngularImpulse = 3.0f;

        /// <summary>
        ///     Tries to throw the entity if it has a physics component, otherwise does nothing.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="direction">Will use the vector's magnitude as the strength of the impulse</param>
        /// <param name="user"></param>
        /// <param name="pushbackRatio">The ratio of impulse applied to the thrower</param>
        internal static void TryThrow(this IEntity entity, Vector2 direction, IEntity? user = null, float pushbackRatio = 1.0f)
        {
            if (entity.Deleted || direction == Vector2.Zero || !entity.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            if (physicsComponent.BodyType == BodyType.Static)
            {
                Logger.Warning("Tried to throw entity {entity} but can't throw static bodies!");
                return;
            }

            if (entity.HasComponent<IMobStateComponent>())
            {
                Logger.Warning("Throwing not supported for mobs!");
                return;
            }

            if (entity.HasComponent<ItemComponent>())
            {
                entity.EnsureComponent<ThrownItemComponent>().Thrower = user;
                // Give it a l'il spin.
                physicsComponent.ApplyAngularImpulse(ThrowAngularImpulse);

                if (user != null)
                    EntitySystem.Get<InteractionSystem>().ThrownInteraction(user, entity);
            }

            physicsComponent.ApplyLinearImpulse(direction);
            // Give thrower an impulse in the other direction
            if (user != null && pushbackRatio > 0.0f && user.TryGetComponent(out IPhysBody? body))
            {
                var msg = new ThrowPushbackAttemptEvent();
                body.Owner.EntityManager.EventBus.RaiseLocalEvent(body.Owner.Uid, msg);

                if (!msg.Cancelled)
                {
                    body.ApplyLinearImpulse(-direction * pushbackRatio);
                }
            }
        }
    }
}

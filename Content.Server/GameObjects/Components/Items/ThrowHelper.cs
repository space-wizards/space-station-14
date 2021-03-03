#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Items
{
    internal static class ThrowHelper
    {
        /// <summary>
        ///     Tries to throw the entity if it has a physics component, otherwise does nothing.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="direction">Will use the vector's magnitude as the strength of the impulse</param>
        internal static void TryThrow(this IEntity entity, Vector2 direction, IEntity? user = null)
        {
            if (direction == Vector2.Zero || !entity.TryGetComponent(out PhysicsComponent? physicsComponent))
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

                if (user != null)
                    EntitySystem.Get<InteractionSystem>().ThrownInteraction(user, entity);
            }

            physicsComponent.ApplyLinearImpulse(direction);
        }
    }
}

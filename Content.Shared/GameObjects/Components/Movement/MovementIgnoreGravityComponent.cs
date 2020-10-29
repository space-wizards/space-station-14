#nullable enable
using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Content.Shared.GameObjects.EntitySystemMessages;

namespace Content.Shared.GameObjects.Components.Movement
{
    [RegisterComponent]
    public sealed class MovementIgnoreGravityComponent : Component
    {
        public override string Name => "MovementIgnoreGravity";
    }

    public static class GravityExtensions
    {
        public static bool IsWeightless(this IEntity entity, IPhysicsManager? physicsManager = null)
        {
            physicsManager ??= IoCManager.Resolve<IPhysicsManager>();

            var isWeightless = !entity.HasComponent<MovementIgnoreGravityComponent>() &&
                   physicsManager.IsWeightless(entity.Transform.Coordinates);
            entity.EntityManager.EventBus.RaiseEvent(EventSource.Local, new WeightlessChangeMessage(entity,isWeightless));
            return isWeightless;
        }
    }
}

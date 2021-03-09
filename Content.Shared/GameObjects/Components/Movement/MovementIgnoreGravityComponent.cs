#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

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

            return !entity.HasComponent<MovementIgnoreGravityComponent>() &&
                   physicsManager.IsWeightless(entity.Transform.Coordinates);
        }
    }
}

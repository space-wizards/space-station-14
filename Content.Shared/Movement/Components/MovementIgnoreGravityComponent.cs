#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
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
        public static bool IsWeightless(this IEntity entity, PhysicsComponent? body = null, EntityCoordinates? coords = null, IMapManager? mapManager = null)
        {
            if (body == null)
                entity.TryGetComponent(out body);

            if (entity.HasComponent<MovementIgnoreGravityComponent>() ||
                (body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0) return false;

            var transform = entity.Transform;
            var gridId = transform.GridID;

            if (!gridId.IsValid())
            {
                // Not on a grid = no gravity for now.
                // In the future, may want to allow maps to override to always have gravity instead.
                return true;
            }

            mapManager ??= IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(gridId);

            if (!grid.HasGravity)
            {
                return true;
            }

            coords ??= transform.Coordinates;

            if (!coords.Value.IsValid(entity.EntityManager))
            {
                return true;
            }

            var tile = grid.GetTileRef(coords.Value).Tile;
            return tile.IsEmpty;
        }
    }
}

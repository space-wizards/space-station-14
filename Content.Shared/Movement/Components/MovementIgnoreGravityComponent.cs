using Content.Shared.Gravity;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Movement.Components
{
    [RegisterComponent]
    public sealed class MovementIgnoreGravityComponent : Component
    {
        public override string Name => "MovementIgnoreGravity";
    }

    public static class GravityExtensions
    {
        public static bool IsWeightless(this EntityUid entity, PhysicsComponent? body = null, EntityCoordinates? coords = null, IMapManager? mapManager = null, IEntityManager? entityManager = null)
        {
            if (body == null)
                IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out body);

            if (IoCManager.Resolve<IEntityManager>().HasComponent<MovementIgnoreGravityComponent>(entity) ||
                (body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0) return false;

            var transform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity);
            var gridId = transform.GridID;

            if (!gridId.IsValid())
            {
                // Not on a grid = no gravity for now.
                // In the future, may want to allow maps to override to always have gravity instead.
                return true;
            }

            mapManager ??= IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(gridId);
            var gridEntityId = grid.GridEntityId;
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            var gridEntity = entityManager.GetEntity(gridEntityId);

            if (!IoCManager.Resolve<IEntityManager>().GetComponent<GravityComponent>(gridEntity).Enabled)
            {
                return true;
            }

            coords ??= transform.Coordinates;

            if (!coords.Value.IsValid(IoCManager.Resolve<IEntityManager>()))
            {
                return true;
            }

            var tile = grid.GetTileRef(coords.Value).Tile;
            return tile.IsEmpty;
        }
    }
}

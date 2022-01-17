using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
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
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (body == null)
                entityManager.TryGetComponent(entity, out body);

            if (entityManager.HasComponent<MovementIgnoreGravityComponent>(entity) ||
                (body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0) return false;

            var transform = entityManager.GetComponent<TransformComponent>(entity);
            var gridId = transform.GridID;

            if (!gridId.IsValid())
            {
                // Not on a grid = no gravity for now.
                // In the future, may want to allow maps to override to always have gravity instead.
                return true;
            }

            mapManager ??= IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(gridId);
            var invSys = EntitySystem.Get<InventorySystem>();

            if (invSys.TryGetSlotEntity(entity, "shoes", out var ent))
            {
                if (entityManager.TryGetComponent<SharedMagbootsComponent>(ent, out var boots) && boots.On)
                    return false;
            }

            if (!entityManager.GetComponent<GravityComponent>(grid.GridEntityId).Enabled)
            {
                return true;
            }

            coords ??= transform.Coordinates;

            if (!coords.Value.IsValid(entityManager))
            {
                return true;
            }

            var tile = grid.GetTileRef(coords.Value).Tile;
            return tile.IsEmpty;
        }
    }
}

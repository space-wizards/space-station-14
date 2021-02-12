#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Utility
{
    public static class EntitySystemExtensions
    {
        public static IEntity? SpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            EntityCoordinates coordinates,
            CollisionGroup collisionLayer,
            Box2? box = null,
            bool approximate = false)
        {
            var mapCoordinates = coordinates.ToMap(entityManager);
            return entityManager.SpawnIfUnobstructed(prototypeName, mapCoordinates, collisionLayer, box, approximate);
        }

        public static IEntity? SpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            MapCoordinates coordinates,
            CollisionGroup collisionLayer,
            Box2? box = null,
            bool approximate = false)
        {
            box ??= Box2.UnitCentered;

            foreach (var entity in entityManager.GetEntitiesIntersecting(coordinates.MapId, box.Value, approximate))
            {
                if (!entity.TryGetComponent(out IPhysicsComponent? physics))
                {
                    continue;
                }

                if (!physics.Hard)
                {
                    continue;
                }

                if (collisionLayer == 0 || (physics.CollisionMask & (int) collisionLayer) == 0)
                {
                    continue;
                }

                return null;
            }

            return entityManager.SpawnEntity(prototypeName, coordinates);
        }

        public static bool TrySpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            EntityCoordinates coordinates,
            CollisionGroup collisionLayer,
            [NotNullWhen(true)] out IEntity? entity,
            Box2? box = null,
            bool approximate = false)
        {
            entity = entityManager.SpawnIfUnobstructed(prototypeName, coordinates, collisionLayer, box, approximate);

            return entity != null;
        }

        public static bool TrySpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            MapCoordinates coordinates,
            CollisionGroup collisionLayer,
            [NotNullWhen(true)] out IEntity? entity,
            Box2? box = null,
            bool approximate = false)
        {
            entity = entityManager.SpawnIfUnobstructed(prototypeName, coordinates, collisionLayer, box, approximate);

            return entity != null;
        }
    }
}

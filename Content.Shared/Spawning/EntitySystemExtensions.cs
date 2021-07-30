using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;

namespace Content.Shared.Spawning
{
    public static class EntitySystemExtensions
    {
        public static IEntity? SpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            EntityCoordinates coordinates,
            CollisionGroup collisionLayer,
            in Box2? box = null,
            SharedBroadphaseSystem? physicsManager = null)
        {
            physicsManager ??= EntitySystem.Get<SharedBroadphaseSystem>();
            var mapCoordinates = coordinates.ToMap(entityManager);

            return entityManager.SpawnIfUnobstructed(prototypeName, mapCoordinates, collisionLayer, box, physicsManager);
        }

        public static IEntity? SpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            MapCoordinates coordinates,
            CollisionGroup collisionLayer,
            in Box2? box = null,
            SharedBroadphaseSystem? collision = null)
        {
            var boxOrDefault = box.GetValueOrDefault(Box2.UnitCentered);
            collision ??= EntitySystem.Get<SharedBroadphaseSystem>();

            foreach (var body in collision.GetCollidingEntities(coordinates.MapId, in boxOrDefault))
            {
                if (!body.Hard)
                {
                    continue;
                }

                // TODO: wtf fix this
                if (collisionLayer == 0 || (body.CollisionMask & (int) collisionLayer) == 0)
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
            SharedBroadphaseSystem? physicsManager = null)
        {
            entity = entityManager.SpawnIfUnobstructed(prototypeName, coordinates, collisionLayer, box, physicsManager);

            return entity != null;
        }

        public static bool TrySpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            MapCoordinates coordinates,
            CollisionGroup collisionLayer,
            [NotNullWhen(true)] out IEntity? entity,
            in Box2? box = null,
            SharedBroadphaseSystem? physicsManager = null)
        {
            entity = entityManager.SpawnIfUnobstructed(prototypeName, coordinates, collisionLayer, box, physicsManager);

            return entity != null;
        }
    }
}

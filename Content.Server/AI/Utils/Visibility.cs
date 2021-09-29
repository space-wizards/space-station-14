using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.AI.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;

namespace Content.Server.AI.Utils
{
    public static class Visibility
    {
        // Just do a simple range check, then chuck the ray out. If we get bigger than 1 tile mobs may need to adjust this
        public static bool InLineOfSight(IEntity owner, IEntity target)
        {
            var range = 50.0f;

            if (owner.Transform.GridID != target.Transform.GridID)
            {
                return false;
            }

            if (owner.TryGetComponent(out AiControllerComponent? controller))
            {
                var targetRange = (target.Transform.Coordinates.Position - owner.Transform.Coordinates.Position).Length;
                if (targetRange > controller.VisionRadius)
                {
                    return false;
                }

                range = controller.VisionRadius;
            }

            var angle = new Angle(target.Transform.Coordinates.Position - owner.Transform.Coordinates.Position);
            var ray = new CollisionRay(
                owner.Transform.Coordinates.Position,
                angle.ToVec(),
                (int)(CollisionGroup.Opaque | CollisionGroup.Impassable | CollisionGroup.MobImpassable));

            var rayCastResults = EntitySystem.Get<SharedBroadphaseSystem>().IntersectRay(owner.Transform.MapID, ray, range, owner).ToList();

            return rayCastResults.Count > 0 && rayCastResults[0].HitEntity == target;
        }

        // Should this be in robust or something? Fark it
        public static IEnumerable<IEntity> GetNearestEntities(EntityCoordinates grid, Type component, float range)
        {
            var inRange = GetEntitiesInRange(grid, component, range).ToList();
            var sortedInRange = inRange.OrderBy(o => (o.Transform.Coordinates.Position - grid.Position).Length);

            return sortedInRange;
        }

        public static IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates grid, Type component, float range)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var entity in entityManager.GetAllComponents(component).Select(c => c.Owner))
            {
                if (entity.Transform.Coordinates.GetGridId(entityManager) != grid.GetGridId(entityManager))
                {
                    continue;
                }

                if ((entity.Transform.Coordinates.Position - grid.Position).Length <= range)
                {
                    yield return entity;
                }
            }
        }
    }
}

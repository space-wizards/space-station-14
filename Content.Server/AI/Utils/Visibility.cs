using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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

            if (owner.TryGetComponent(out AiControllerComponent controller))
            {
                var targetRange = (target.Transform.GridPosition.Position - owner.Transform.GridPosition.Position).Length;
                if (targetRange > controller.VisionRadius)
                {
                    return false;
                }

                range = controller.VisionRadius;
            }

            var angle = new Angle(target.Transform.GridPosition.Position - owner.Transform.GridPosition.Position);
            var ray = new Ray(
                owner.Transform.GridPosition.Position,
                angle.ToVec(),
                (int)(CollisionGroup.Opaque | CollisionGroup.Impassable | CollisionGroup.MobImpassable));

            var rayCastResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(owner.Transform.MapID, ray, range, owner);

            return rayCastResults.HitEntity == target;
        }

        // Should this be in robust or something? Fark it
        public static IEnumerable<IEntity> GetNearestEntities(GridCoordinates grid, Type component, float range)
        {
            var inRange = GetEntitiesInRange(grid, component, range).ToList();

            var sortedInRange = inRange.OrderBy(o => (o.Transform.GridPosition.Position - grid.Position).Length);

            return sortedInRange;
        }

        public static IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates grid, Type component, float range)
        {
            var compManager = IoCManager.Resolve<IComponentManager>();
            foreach (var comp in compManager.GetAllComponents(component))
            {
                if (comp.Owner.Transform.GridPosition.GridID != grid.GridID)
                {
                    continue;
                }

                if ((comp.Owner.Transform.GridPosition.Position - grid.Position).Length <= range)
                {
                    yield return comp.Owner;
                }
            }
        }
    }
}

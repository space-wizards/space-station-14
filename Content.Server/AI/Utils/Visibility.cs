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

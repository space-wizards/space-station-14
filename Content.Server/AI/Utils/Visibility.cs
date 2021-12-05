using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.AI.Utils
{
    public static class Visibility
    {
        // Should this be in robust or something? Fark it
        public static IEnumerable<EntityUid> GetNearestEntities(EntityCoordinates grid, Type component, float range)
        {
            var inRange = GetEntitiesInRange(grid, component, range).ToList();
            var sortedInRange = inRange.OrderBy(o => (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(o).Coordinates.Position - grid.Position).Length);

            return sortedInRange;
        }

        public static IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates grid, Type component, float range)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var entity in entityManager.GetAllComponents(component).Select(c => c.Owner))
            {
                if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).Coordinates.GetGridId(entityManager) != grid.GetGridId(entityManager))
                {
                    continue;
                }

                if ((IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).Coordinates.Position - grid.Position).Length <= range)
                {
                    yield return entity;
                }
            }
        }
    }
}

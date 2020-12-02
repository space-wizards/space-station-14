using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    public class WallInTile : IConstructionCondition
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            // first let's check that we don't try to build on other side of the wall
            var userWorldPosition = user.Transform.WorldPosition;
            var objWorldPosition = location.ToMap(entManager).Position;

            var dif = (objWorldPosition - userWorldPosition);
            var dotProd = Vector2.Dot(direction.ToVec(), dif);

            if (dotProd > 0)
                return false;

            // lets check direction
            foreach (var entity in location.GetEntitiesInTile(true))
            {
                if (entity.HasComponent<SharedWallComponent>())
                {
                    // we found wall - but does it face the right side?
                    // lets check the tile that fixture facing
                    var dirLocation = location.Offset(direction);

                    foreach (var nearEnt in dirLocation.GetEntitiesInTile(true))
                    {
                        if (nearEnt.HasComponent<SharedWallComponent>())
                            return false;
                    }

                    return true;
                }
            }




            return false;
        }
    }
}

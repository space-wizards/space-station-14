#nullable enable
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Construction.ConstructionConditions
{
    public class WallmountCondition : IConstructionCondition
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            // check that player doesn't try to build object facing wall other side
            bool invalidDirection = FacingWallOtherSide(user, location, direction);
            if (invalidDirection)
                return false;

            if (direction != Direction.North)
            {
                // check that building inside wall
                var wallEnity = location.GetEntitiesInTile(true)
                    .FirstOrDefault((e) => e.HasComponent<SharedWallComponent>());
                if (wallEnity == null)
                    return false;
            }
            else
            {
                // north is exception because of world projection
                // wallmount wil be build one tile above the wall
                var belowLocation = location.Offset(Direction.South);
                var wallEnity = belowLocation.GetEntitiesInTile(true)
                    .FirstOrDefault((e) => e.HasComponent<SharedWallComponent>());
                if (wallEnity == null)
                    return false;
            }

            // check that building doesn't facing adjacent wall
            var dirLocation = location.Offset(direction);
            var adjWallEntity = dirLocation.GetEntitiesInTile(true)
                .FirstOrDefault((e) => e.HasComponent<SharedWallComponent>());
            if (adjWallEntity != null)
                return false;

            // TODO: check that we doesn't intersect other wallmount

            return true;
        }

        private bool FacingWallOtherSide(IEntity user, EntityCoordinates location, Direction bpDirection)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            // get blueprint and user position
            var userWorldPosition = user.Transform.WorldPosition;
            var objWorldPosition = location.ToMap(entManager).Position;

            // find direction from user to blueprint
            var userToObject = (objWorldPosition - userWorldPosition);

            // dot product will be positive if user direction and blueprint are co-directed
            var dotProd = Vector2.Dot(bpDirection.ToVec(), userToObject);
            return dotProd > 0;
        }
    }
}

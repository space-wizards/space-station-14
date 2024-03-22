using System.Linq;
using System.Numerics;
using Content.Shared.Physics;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class WallmountCondition : IConstructionCondition
    {
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            // get blueprint and user position
            var transformSystem = entManager.System<SharedTransformSystem>();
            var userWorldPosition = entManager.GetComponent<TransformComponent>(user).WorldPosition;
            var objWorldPosition = location.ToMap(entManager, transformSystem).Position;

            // find direction from user to blueprint
            var userToObject = (objWorldPosition - userWorldPosition);
            // get direction of the grid being placed on as an offset.
            var gridRotation = entManager.GetComponent<TransformComponent>(location.EntityId).WorldRotation;
            var directionWithOffset = gridRotation.RotateVec(direction.ToVec());

            // dot product will be positive if user direction and blueprint are co-directed
            var dotProd = Vector2.Dot(directionWithOffset.Normalized(), userToObject.Normalized());
            if (dotProd > 0)
                return false;

            // now we need to check that user actually tries to build wallmount on a wall
            var physics = entManager.System<SharedPhysicsSystem>();
            var rUserToObj = new CollisionRay(userWorldPosition, userToObject.Normalized(), (int) CollisionGroup.Impassable);
            var length = userToObject.Length();

            var tagSystem = entManager.System<TagSystem>();

            var userToObjRaycastResults = physics.IntersectRayWithPredicate(entManager.GetComponent<TransformComponent>(user).MapID, rUserToObj, maxLength: length,
                predicate: (e) => !tagSystem.HasTag(e, "Wall"));

            var targetWall = userToObjRaycastResults.FirstOrNull();

            if (targetWall == null)
                return false;

            // get this wall entity
            // check that we didn't try to build wallmount that facing another adjacent wall
            var rAdjWall = new CollisionRay(objWorldPosition, directionWithOffset.Normalized(), (int) CollisionGroup.Impassable);
            var adjWallRaycastResults = physics.IntersectRayWithPredicate(entManager.GetComponent<TransformComponent>(user).MapID, rAdjWall, maxLength: 0.5f,
               predicate: e => e == targetWall.Value.HitEntity || !tagSystem.HasTag(e, "Wall"));

            return !adjWallRaycastResults.Any();
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry()
            {
                Localization = "construction-step-condition-wallmount",
            };
        }
    }
}

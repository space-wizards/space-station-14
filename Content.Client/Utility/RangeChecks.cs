using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.EntitySystems.SharedInteractionSystem;

namespace Content.Client.Utility
{
    public static class RangeChecks
    {
        private static SharedInteractionSystem SharedInteractionSystem => EntitySystem.Get<SharedInteractionSystem>();

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originEntity = origin.ControlledEntity;
            if (originEntity == null)
            {
                // TODO: Take into account the player's camera position?
                return false;
            }

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originEntity = origin.ControlledEntity;
            if (originEntity == null)
            {
                // TODO: Take into account the player's camera position?
                return false;
            }

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            GridCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originEntity = origin.ControlledEntity;
            if (originEntity == null)
            {
                // TODO: Take into account the player's camera position?
                return false;
            }

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originEntity = origin.ControlledEntity;
            if (originEntity == null)
            {
                // TODO: Take into account the player's camera position?
                return false;
            }

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate, ignoreInsideBlocker);
        }
    }
}

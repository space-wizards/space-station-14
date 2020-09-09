using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.EntitySystems.SharedInteractionSystem;

namespace Content.Client.Utility
{
    public static class RangeExtensions
    {
        private static SharedInteractionSystem SharedInteractionSystem => EntitySystem.Get<SharedInteractionSystem>();

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var otherPosition = other.Transform.MapPosition;

            return origin.InRangeUnobstructed(otherPosition, range, collisionMask, predicate, ignoreInsideBlocker,
                popup);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return origin.InRangeUnobstructed(other.Owner, range, collisionMask, predicate, ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return origin.InRangeUnobstructed(other.Owner, range, collisionMask, predicate, ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var otherPosition = other.ToMap(entityManager);

            return origin.InRangeUnobstructed(otherPosition, range, collisionMask, predicate, ignoreInsideBlocker,
                popup);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.ControlledEntity;
            if (originEntity == null)
            {
                // TODO: Take into account the player's camera position?
                return false;
            }

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
    }
}

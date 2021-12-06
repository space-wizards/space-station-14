using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Client.Interactable
{
    public static class UnobstructedExtensions
    {
        private static SharedInteractionSystem SharedInteractionSystem => EntitySystem.Get<SharedInteractionSystem>();

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            EntityUid other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var otherPosition = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(other).MapPosition;

            return origin.InRangeUnobstructed(otherPosition, range, collisionMask, predicate, ignoreInsideBlocker,
                popup);
        }

        public static bool InRangeUnobstructed(
            this LocalPlayer origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
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
            Ignored? predicate = null,
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
            Ignored? predicate = null,
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
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.ControlledEntity;
            if (originEntity == null)
            {
                // TODO: Take into account the player's camera position?
                return false;
            }

            return SharedInteractionSystem.InRangeUnobstructed(originEntity.Value, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
    }
}

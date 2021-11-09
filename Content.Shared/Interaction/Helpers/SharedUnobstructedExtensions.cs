using Content.Shared.DragDrop;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared.Interaction.Helpers
{
    // TODO: Kill these with fire.
    public static class SharedUnobstructedExtensions
    {
        private static SharedInteractionSystem SharedInteractionSystem => EntitySystem.Get<SharedInteractionSystem>();

        #region Entities
        public static bool InRangeUnobstructed(
            this IEntity origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(origin, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this EntityUid origin,
            EntityUid other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false,
            IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            return InRangeUnobstructed(entityManager.GetEntity(origin), entityManager.GetEntity(other),
                range, collisionMask, predicate, ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IEntity origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(origin, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IEntity origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var otherEntity = other.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherEntity, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IEntity origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(origin, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IEntity origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(origin, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
        #endregion

        #region Components
        public static bool InRangeUnobstructed(
            this IComponent origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IComponent origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IComponent origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;
            var otherEntity = other.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, otherEntity, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IComponent origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IComponent origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
        #endregion

        #region Containers
        public static bool InRangeUnobstructed(
            this IContainer origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this IContainer origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IContainer origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;
            var otherEntity = other.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, otherEntity, range, collisionMask,
                predicate, ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IContainer origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IContainer origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
        #endregion

        #region EntityCoordinates
        public static bool InRangeUnobstructed(
            this EntityCoordinates origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originPosition = origin.ToMap(other.EntityManager);
            var otherPosition = other.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this EntityCoordinates origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originPosition = origin.ToMap(other.Owner.EntityManager);
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this EntityCoordinates origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var originPosition = origin.ToMap(other.Owner.EntityManager);
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this EntityCoordinates origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var originPosition = origin.ToMap(entityManager);
            var otherPosition = other.ToMap(entityManager);

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this EntityCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var originPosition = origin.ToMap(entityManager);

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, other, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }
        #endregion

        #region MapCoordinates
        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var otherPosition = other.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherPosition, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherPosition, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherPosition, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var otherPosition = other.ToMap(entityManager);

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherPosition, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(origin, other, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }
        #endregion

        #region EventArgs
        public static bool InRangeUnobstructed(
            this ITargetedInteractEventArgs args,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(args.User, args.Target, range, collisionMask, predicate, ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this AfterInteractEventArgs args,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var user = args.User;
            var target = args.Target;

            if (target == null)
                return SharedInteractionSystem.InRangeUnobstructed(user, args.ClickLocation, range, collisionMask, predicate, ignoreInsideBlocker, popup);
            else
                return SharedInteractionSystem.InRangeUnobstructed(user, target, range, collisionMask, predicate, ignoreInsideBlocker, popup);

        }
        #endregion

        #region EntityEventArgs
        public static bool InRangeUnobstructed(
            this DragDropEvent args,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var user = args.User;
            var dropped = args.Dragged;
            var target = args.Target;

            if (!SharedInteractionSystem.InRangeUnobstructed(user, target, range, collisionMask, predicate, ignoreInsideBlocker, popup))
                return false;

            if (!SharedInteractionSystem.InRangeUnobstructed(user, dropped, range, collisionMask, predicate, ignoreInsideBlocker, popup))
                return false;

            return true;
        }

        public static bool InRangeUnobstructed(
            this AfterInteractEvent args,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var user = args.User;
            var target = args.Target;

            if (target == null)
                return SharedInteractionSystem.InRangeUnobstructed(user, args.ClickLocation, range, collisionMask, predicate, ignoreInsideBlocker, popup);
            else
                return SharedInteractionSystem.InRangeUnobstructed(user, target, range, collisionMask, predicate, ignoreInsideBlocker, popup);
        }
        #endregion
    }
}

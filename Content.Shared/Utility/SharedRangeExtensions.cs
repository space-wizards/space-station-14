using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.EntitySystems.SharedInteractionSystem;

namespace Content.Shared.Utility
{
    public static class SharedRangeExtensions
    {
        private static SharedInteractionSystem SharedInteractionSystem => EntitySystem.Get<SharedInteractionSystem>();

        #region Entities
        public static bool InRangeUnobstructed(
            this IEntity origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(origin, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IEntity origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
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
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var otherEntity = other.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherEntity, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this IEntity origin,
            GridCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            GridCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            GridCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
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
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            var originEntity = origin.Owner;

            return SharedInteractionSystem.InRangeUnobstructed(originEntity, other, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
        #endregion

        #region GridCoordinates
        public static bool InRangeUnobstructed(
            this GridCoordinates origin,
            IEntity other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var originPosition = origin.ToMap(mapManager);
            var otherPosition = other.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this GridCoordinates origin,
            IComponent other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var originPosition = origin.ToMap(mapManager);
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this GridCoordinates origin,
            IContainer other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var originPosition = origin.ToMap(mapManager);
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this GridCoordinates origin,
            GridCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var originPosition = origin.ToMap(mapManager);
            var otherPosition = other.ToMap(mapManager);

            return SharedInteractionSystem.InRangeUnobstructed(originPosition, otherPosition, range, collisionMask,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this GridCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var originPosition = origin.ToMap(mapManager);

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
            Ignored predicate = null,
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
            Ignored predicate = null,
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
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var otherPosition = other.Owner.Transform.MapPosition;

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherPosition, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            GridCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var otherPosition = other.ToMap(mapManager);

            return SharedInteractionSystem.InRangeUnobstructed(origin, otherPosition, range, collisionMask, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnobstructed(
            this MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
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
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(args, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this DragDropEventArgs args,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(args, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }

        public static bool InRangeUnobstructed(
            this AfterInteractEventArgs args,
            float range = InteractionRange,
            CollisionGroup collisionMask = CollisionGroup.Impassable,
            Ignored predicate = null,
            bool ignoreInsideBlocker = false,
            bool popup = false)
        {
            return SharedInteractionSystem.InRangeUnobstructed(args, range, collisionMask, predicate,
                ignoreInsideBlocker, popup);
        }
        #endregion
    }
}

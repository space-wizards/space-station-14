using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using static Content.Shared.Examine.ExamineSystemShared;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared.Interaction.Helpers
{
    public static class SharedUnoccludedExtensions
    {
        #region Entities
        public static bool InRangeUnOccluded(
            this EntityUid origin,
            EntityUid other,
            float range = ExamineRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            return ExamineSystemShared.InRangeUnOccluded(origin, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityUid origin,
            IComponent other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            return ExamineSystemShared.InRangeUnOccluded(origin, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityUid origin,
            BaseContainer other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var otherEntity = other.Owner;

            return ExamineSystemShared.InRangeUnOccluded(origin, otherEntity, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityUid origin,
            EntityCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            return ExamineSystemShared.InRangeUnOccluded(origin, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityUid origin,
            MapCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            return ExamineSystemShared.InRangeUnOccluded(origin, other, range, predicate, ignoreInsideBlocker);
        }
        #endregion

        #region Components
        public static bool InRangeUnOccluded(
            this IComponent origin,
            EntityUid other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this IComponent origin,
            IComponent other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this IComponent origin,
            BaseContainer other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;
            var otherEntity = other.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, otherEntity, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this IComponent origin,
            EntityCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this IComponent origin,
            MapCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate,
                ignoreInsideBlocker);
        }
        #endregion

        #region Containers
        public static bool InRangeUnOccluded(
            this BaseContainer origin,
            EntityUid other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this BaseContainer origin,
            IComponent other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this BaseContainer origin,
            BaseContainer other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;
            var otherEntity = other.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, otherEntity, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this BaseContainer origin,
            EntityCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this BaseContainer origin,
            MapCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var originEntity = origin.Owner;

            return ExamineSystemShared.InRangeUnOccluded(originEntity, other, range, predicate, ignoreInsideBlocker);
        }
        #endregion

        #region EntityCoordinates
        public static bool InRangeUnOccluded(
            this EntityCoordinates origin,
            EntityUid other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPosition = origin.ToMap(entMan);
            var otherPosition = entMan.GetComponent<TransformComponent>(other).MapPosition;

            return ExamineSystemShared.InRangeUnOccluded(originPosition, otherPosition, range,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityCoordinates origin,
            IComponent other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPosition = origin.ToMap(entMan);
            var otherPosition = entMan.GetComponent<TransformComponent>(other.Owner).MapPosition;

            return ExamineSystemShared.InRangeUnOccluded(originPosition, otherPosition, range,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityCoordinates origin,
            BaseContainer other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPosition = origin.ToMap(entMan);
            var otherPosition = entMan.GetComponent<TransformComponent>(other.Owner).MapPosition;

            return ExamineSystemShared.InRangeUnOccluded(originPosition, otherPosition, range,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityCoordinates origin,
            EntityCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true,
            IEntityManager? entityManager = null)
        {
            IoCManager.Resolve(ref entityManager);

            var originPosition = origin.ToMap(entityManager);
            var otherPosition = other.ToMap(entityManager);

            return ExamineSystemShared.InRangeUnOccluded(originPosition, otherPosition, range,
                predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this EntityCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true,
            IEntityManager? entityManager = null)
        {
            IoCManager.Resolve(ref entityManager);

            var originPosition = origin.ToMap(entityManager);

            return ExamineSystemShared.InRangeUnOccluded(originPosition, other, range, predicate,
                ignoreInsideBlocker);
        }
        #endregion

        #region MapCoordinates
        public static bool InRangeUnOccluded(
            this MapCoordinates origin,
            EntityUid other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var otherPosition = entMan.GetComponent<TransformComponent>(other).MapPosition;

            return ExamineSystemShared.InRangeUnOccluded(origin, otherPosition, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this MapCoordinates origin,
            IComponent other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var otherPosition = entMan.GetComponent<TransformComponent>(other.Owner).MapPosition;

            return ExamineSystemShared.InRangeUnOccluded(origin, otherPosition, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this MapCoordinates origin,
            BaseContainer other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var otherPosition = entMan.GetComponent<TransformComponent>(other.Owner).MapPosition;

            return ExamineSystemShared.InRangeUnOccluded(origin, otherPosition, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this MapCoordinates origin,
            EntityCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true,
            IEntityManager? entityManager = null)
        {
            IoCManager.Resolve(ref entityManager);

            var otherPosition = other.ToMap(entityManager);

            return ExamineSystemShared.InRangeUnOccluded(origin, otherPosition, range, predicate,
                ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(
            this MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            Ignored? predicate = null,
            bool ignoreInsideBlocker = true)
        {
            return ExamineSystemShared.InRangeUnOccluded(origin, other, range, predicate,
                ignoreInsideBlocker);
        }
        #endregion
    }
}

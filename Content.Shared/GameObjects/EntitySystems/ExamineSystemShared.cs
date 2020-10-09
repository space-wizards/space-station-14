using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.EntitySystems.SharedInteractionSystem;

namespace Content.Shared.GameObjects.EntitySystems
{
    public interface IExamine
    {
        /// <summary>
        /// Returns a status examine value for components appended to the end of the description of the entity
        /// </summary>
        /// <param name="message">The message to append to which will be displayed.</param>
        /// <param name="inDetailsRange">Whether the examiner is within the 'Details' range, allowing you to show information logically only availabe when close to the examined entity.</param>
        void Examine(FormattedMessage message, bool inDetailsRange);
    }
    public abstract class ExamineSystemShared : EntitySystem
    {
        public const float ExamineRange = 16f;
        public const float ExamineRangeSquared = ExamineRange * ExamineRange;
        protected const float ExamineDetailsRange = 3f;

        private static bool IsInDetailsRange(IEntity examiner, IEntity entity)
        {
            return examiner.InRangeUnobstructed(entity, ExamineDetailsRange, ignoreInsideBlocker: true) &&
                   examiner.IsInSameOrNoContainer(entity);
        }

        [Pure]
        protected static bool CanExamine(IEntity examiner, IEntity examined)
        {
            if (!examiner.TryGetComponent(out ExaminerComponent examinerComponent))
            {
                return false;
            }

            if (!examinerComponent.DoRangeCheck)
            {
                return true;
            }

            if (examiner.Transform.MapID != examined.Transform.MapID)
            {
                return false;
            }

            Ignored predicate = entity => entity == examiner || entity == examined;

            if (ContainerHelpers.TryGetContainer(examiner, out var container))
            {
                predicate += entity => entity == container.Owner;
            }

            return InRangeUnOccluded(
                examiner.Transform.MapPosition,
                examined.Transform.MapPosition,
                ExamineRange,
                predicate: predicate,
                ignoreInsideBlocker: true);
        }

        public static bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored predicate, bool ignoreInsideBlocker = true)
        {
            var occluderSystem = Get<OccluderSystem>();
            if (!origin.InRange(other, range)) return false;

            var dir = other.Position - origin.Position;

            if (dir.LengthSquared.Equals(0f)) return true;
            if (range > 0f && !(dir.LengthSquared <= range * range)) return false;

            predicate ??= _ => false;

            var ray = new Ray(origin.Position, dir.Normalized);
            var rayResults = occluderSystem
                .IntersectRayWithPredicate(origin.MapId, ray, dir.Length, predicate.Invoke, false).ToList();

            if (rayResults.Count == 0) return true;

            if (!ignoreInsideBlocker) return false;

            if (rayResults.Count <= 0) return false;

            return (rayResults[0].HitPos - other.Position).Length < 1f;
        }

        public static FormattedMessage GetExamineText(IEntity entity, IEntity examiner)
        {
            var message = new FormattedMessage();

            var doNewline = false;

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(entity.Description))
            {
                message.AddText(entity.Description);
                doNewline = true;
            }

            message.PushColor(Color.DarkGray);

            //Add component statuses from components that report one
            foreach (var examineComponent in entity.GetAllComponents<IExamine>())
            {
                var subMessage = new FormattedMessage();
                examineComponent.Examine(subMessage, IsInDetailsRange(examiner, entity));
                if (subMessage.Tags.Count == 0)
                    continue;

                if (doNewline)
                    message.AddText("\n");

                message.AddMessage(subMessage);
                doNewline = true;
            }

            message.Pop();

            return message;
        }
    }
}

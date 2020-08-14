using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

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

            return Get<SharedInteractionSystem>()
                .InRangeUnobstructed(examiner.Transform.MapPosition, examined.Transform.MapPosition,
                    ExamineRange, predicate: entity => entity == examiner || entity == examined, ignoreInsideBlocker:true);
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

            var inDetailsRange = Get<SharedInteractionSystem>()
                .InRangeUnobstructed(examiner.Transform.MapPosition, entity.Transform.MapPosition,
                    ExamineDetailsRange, predicate: entity0 => entity0 == examiner || entity0 == entity, ignoreInsideBlocker: true);

            //Add component statuses from components that report one
            foreach (var examineComponent in entity.GetAllComponents<IExamine>())
            {
                var subMessage = new FormattedMessage();
                examineComponent.Examine(subMessage, inDetailsRange);
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

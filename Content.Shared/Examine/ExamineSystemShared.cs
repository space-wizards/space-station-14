using System;
using System.Linq;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared.Examine
{
    [Obsolete("Use ExaminedEvent instead.")]
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
            if (!examiner.TryGetComponent(out ExaminerComponent? examinerComponent))
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

            if (examiner.TryGetContainer(out var container))
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

        public static bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            if (origin.MapId == MapId.Nullspace ||
                other.MapId == MapId.Nullspace) return false;

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

            foreach (var result in rayResults)
            {
                if (!result.HitEntity.TryGetComponent(out OccluderComponent? o))
                {
                    continue;
                }

                var bBox = o.BoundingBox.Translated(o.Owner.Transform.WorldPosition);

                if (bBox.Contains(origin.Position) || bBox.Contains(other.Position))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool InRangeUnOccluded(IEntity origin, IEntity other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = origin.Transform.MapPosition;
            var otherPos = other.Transform.MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(IEntity origin, IComponent other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = origin.Transform.MapPosition;
            var otherPos = other.Owner.Transform.MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(IEntity origin, EntityCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = origin.Transform.MapPosition;
            var otherPos = other.ToMap(origin.EntityManager);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(IEntity origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = origin.Transform.MapPosition;

            return InRangeUnOccluded(originPos, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(ITargetedInteractEventArgs args, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = args.User.Transform.MapPosition;
            var otherPos = args.Target.Transform.MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(DragDropEvent args, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = args.User.Transform.MapPosition;
            var otherPos = args.DropLocation.ToMap(args.User.EntityManager);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(AfterInteractEventArgs args, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var originPos = args.User.Transform.MapPosition;
            var otherPos = args.Target?.Transform.MapPosition ?? args.ClickLocation.ToMap(args.User.EntityManager);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public FormattedMessage GetExamineText(IEntity entity, IEntity? examiner)
        {
            var message = new FormattedMessage();

            if (examiner == null)
            {
                return message;
            }

            var doNewline = false;

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(entity.Description))
            {
                message.AddText(entity.Description);
                doNewline = true;
            }

            message.PushColor(Color.DarkGray);

            // Raise the event and let things that subscribe to it change the message...
            var isInDetailsRange = IsInDetailsRange(examiner, entity);
            var examinedEvent = new ExaminedEvent(message, entity, examiner, isInDetailsRange, doNewline);
            RaiseLocalEvent(entity.Uid, examinedEvent);

            //Add component statuses from components that report one
            foreach (var examineComponent in entity.GetAllComponents<IExamine>())
            {
                var subMessage = new FormattedMessage();
                examineComponent.Examine(subMessage, isInDetailsRange);
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

    /// <summary>
    ///     Raised when an entity is examined.
    /// </summary>
    public class ExaminedEvent : EntityEventArgs
    {
        /// <summary>
        ///     The message that will be displayed as the examine text.
        /// For most use cases, you probably want to use <see cref="PushMarkup"/> and similar instead to modify this,
        /// since it handles newlines and such correctly.
        /// </summary>
        /// <seealso cref="PushMessage"/>
        /// <seealso cref="PushMarkup"/>
        /// <seealso cref="PushText"/>
        public FormattedMessage Message { get; }

        /// <summary>
        ///     The entity performing the examining.
        /// </summary>
        public IEntity Examiner { get; }

        /// <summary>
        ///     Entity being examined, for broadcast event purposes.
        /// </summary>
        public IEntity Examined { get; }

        /// <summary>
        ///     Whether the examiner is in range of the entity to get some extra details.
        /// </summary>
        public bool IsInDetailsRange { get; }

        private bool _doNewLine;

        public ExaminedEvent(FormattedMessage message, IEntity examined, IEntity examiner, bool isInDetailsRange, bool doNewLine)
        {
            Message = message;
            Examined = examined;
            Examiner = examiner;
            IsInDetailsRange = isInDetailsRange;
            _doNewLine = doNewLine;
        }

        /// <summary>
        /// Push another message into this examine result, on its own line.
        /// </summary>
        /// <seealso cref="PushMarkup"/>
        /// <seealso cref="PushText"/>
        public void PushMessage(FormattedMessage message)
        {
            if (message.Tags.Count == 0)
                return;

            if (_doNewLine)
                Message.AddText("\n");

            Message.AddMessage(message);
            _doNewLine = true;
        }

        /// <summary>
        /// Push another message parsed from markup into this examine result, on its own line.
        /// </summary>
        /// <seealso cref="PushText"/>
        /// <seealso cref="PushMessage"/>
        public void PushMarkup(string markup)
        {
            PushMessage(FormattedMessage.FromMarkup(markup));
        }

        /// <summary>
        /// Push another message containing raw text into this examine result, on its own line.
        /// </summary>
        /// <seealso cref="PushMarkup"/>
        /// <seealso cref="PushMessage"/>
        public void PushText(string text)
        {
            var msg = new FormattedMessage();
            msg.AddText(text);
            PushMessage(msg);
        }
    }
}

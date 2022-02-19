using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public const float MaxRaycastRange = 100;

        /// <summary>
        ///     Examine range to use when the examiner is in critical condition.
        /// </summary>
        /// <remarks>
        ///     Detailed examinations are disabled while incapactiated. Ideally this should just be set equal to the
        ///     radius of the crit overlay that blackens most of the screen. The actual radius of that is defined
        ///     in a shader sooo... eh.
        /// </remarks>
        public const float CritExamineRange = 1.3f;

        /// <summary>
        ///     Examine range to use when the examiner is dead. See <see cref="CritExamineRange"/>.
        /// </summary>
        public const float DeadExamineRange = 0.75f;

        public const float ExamineRange = 16f;
        protected const float ExamineDetailsRange = 3f;

        /// <summary>
        ///     Creates a new examine tooltip with arbitrary info.
        /// </summary>
        public abstract void SendExamineTooltip(EntityUid player, EntityUid target, FormattedMessage message, bool getVerbs, bool centerAtCursor);

        public bool IsInDetailsRange(EntityUid examiner, EntityUid entity)
        {
            // check if the mob is in ciritcal or dead
            if (EntityManager.TryGetComponent(examiner, out MobStateComponent mobState) && mobState.IsIncapacitated())
                return false;

            if (!_interactionSystem.InRangeUnobstructed(examiner, entity, ExamineDetailsRange))
                return false;

            // Is the target hidden in a opaque locker or something? Currently this check allows players to examine
            // their organs, if they can somehow target them. Really this should be with userSeeInsideSelf: false, and a
            // separate check for if the item is in their inventory or hands.
            if (_containerSystem.IsInSameOrTransparentContainer(examiner, entity, userSeeInsideSelf: true))
                return true;

            // is it inside of an open storage (e.g., an open backpack)?
            return _interactionSystem.CanAccessViaStorage(examiner, entity);
        }

        [Pure]
        public bool CanExamine(EntityUid examiner, EntityUid examined)
        {
            return !Deleted(examined) && CanExamine(examiner, EntityManager.GetComponent<TransformComponent>(examined).MapPosition,
                entity => entity == examiner || entity == examined);
        }

        [Pure]
        public virtual bool CanExamine(EntityUid examiner, MapCoordinates target, Ignored? predicate = null)
        {
            if (!EntityManager.TryGetComponent(examiner, out ExaminerComponent? examinerComponent))
                return false;

            if (!examinerComponent.DoRangeCheck)
                return true;

            if (EntityManager.GetComponent<TransformComponent>(examiner).MapID != target.MapId)
                return false;

            return InRangeUnOccluded(
                EntityManager.GetComponent<TransformComponent>(examiner).MapPosition,
                target,
                GetExaminerRange(examiner),
                predicate: predicate,
                ignoreInsideBlocker: true);
        }

        /// <summary>
        ///     Check if a given examiner is incapacitated. If yes, return a reduced examine range. Otherwise, return the deault range.
        /// </summary>
        public float GetExaminerRange(EntityUid examiner, MobStateComponent? mobState = null)
        {
            if (Resolve(examiner, ref mobState, logMissing: false))
            {
                if (mobState.IsDead())
                    return DeadExamineRange;
                else if (mobState.IsCritical())
                    return CritExamineRange;
            }
            return ExamineRange;
        }

        public static bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            if (other.MapId != origin.MapId ||
                other.MapId == MapId.Nullspace) return false;

            var dir = other.Position - origin.Position;
            var length = dir.Length;

            // If range specified also check it
            if (range > 0f && length > range) return false;

            if (MathHelper.CloseTo(length, 0)) return true;

            if (length > MaxRaycastRange)
            {
                Logger.Warning("InRangeUnOccluded check performed over extreme range. Limiting CollisionRay size.");
                length = MaxRaycastRange;
            }

            var occluderSystem = Get<OccluderSystem>();
            var entMan = IoCManager.Resolve<IEntityManager>();

            predicate ??= _ => false;

            var ray = new Ray(origin.Position, dir.Normalized);
            var rayResults = occluderSystem
                .IntersectRayWithPredicate(origin.MapId, ray, length, predicate.Invoke, false).ToList();

            if (rayResults.Count == 0) return true;

            if (!ignoreInsideBlocker) return false;

            foreach (var result in rayResults)
            {
                if (!entMan.TryGetComponent(result.HitEntity, out OccluderComponent? o))
                {
                    continue;
                }

                var bBox = o.BoundingBox.Translated(entMan.GetComponent<TransformComponent>(o.Owner).WorldPosition);

                if (bBox.Contains(origin.Position) || bBox.Contains(other.Position))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool InRangeUnOccluded(EntityUid origin, EntityUid other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;
            var otherPos = entMan.GetComponent<TransformComponent>(other).MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(EntityUid origin, IComponent other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;
            var otherPos = entMan.GetComponent<TransformComponent>(other.Owner).MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(EntityUid origin, EntityCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;
            var otherPos = other.ToMap(entMan);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(EntityUid origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;

            return InRangeUnOccluded(originPos, other, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(ITargetedInteractEventArgs args, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(args.User).MapPosition;
            var otherPos = entMan.GetComponent<TransformComponent>(args.Target).MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(DragDropEvent args, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(args.User).MapPosition;
            var otherPos = args.DropLocation.ToMap(entMan);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(AfterInteractEventArgs args, float range, Ignored? predicate, bool ignoreInsideBlocker = true)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();;
            var originPos = entityManager.GetComponent<TransformComponent>(args.User).MapPosition;
            var target = args.Target;
            var otherPos = (target != null ? entityManager.GetComponent<TransformComponent>(target.Value).MapPosition : args.ClickLocation.ToMap(entityManager));

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public FormattedMessage GetExamineText(EntityUid entity, EntityUid? examiner)
        {
            var message = new FormattedMessage();

            if (examiner == null)
            {
                return message;
            }

            var doNewline = false;

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(EntityManager.GetComponent<MetaDataComponent>(entity).EntityDescription))
            {
                message.AddText(EntityManager.GetComponent<MetaDataComponent>(entity).EntityDescription);
                doNewline = true;
            }

            message.PushColor(Color.DarkGray);

            // Raise the event and let things that subscribe to it change the message...
            var isInDetailsRange = IsInDetailsRange(examiner.Value, entity);
            var examinedEvent = new ExaminedEvent(message, entity, examiner.Value, isInDetailsRange, doNewline);
            RaiseLocalEvent(entity, examinedEvent);

            //Add component statuses from components that report one
            foreach (var examineComponent in EntityManager.GetComponents<IExamine>(entity))
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
    public sealed class ExaminedEvent : EntityEventArgs
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
        public EntityUid Examiner { get; }

        /// <summary>
        ///     Entity being examined, for broadcast event purposes.
        /// </summary>
        public EntityUid Examined { get; }

        /// <summary>
        ///     Whether the examiner is in range of the entity to get some extra details.
        /// </summary>
        public bool IsInDetailsRange { get; }

        private bool _doNewLine;

        public ExaminedEvent(FormattedMessage message, EntityUid examined, EntityUid examiner, bool isInDetailsRange, bool doNewLine)
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

using System.Linq;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared.Examine
{
    public abstract partial class ExamineSystemShared : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] protected readonly MobStateSystem MobStateSystem = default!;

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

        protected const float ExamineBlurrinessMult = 2.5f;

        /// <summary>
        ///     Creates a new examine tooltip with arbitrary info.
        /// </summary>
        public abstract void SendExamineTooltip(EntityUid player, EntityUid target, FormattedMessage message, bool getVerbs, bool centerAtCursor);

        public bool IsInDetailsRange(EntityUid examiner, EntityUid entity)
        {
            if (IsClientSide(entity))
                return true;

            // check if the mob is in critical or dead
            if (MobStateSystem.IsIncapacitated(examiner))
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
            // special check for client-side entities stored in null-space for some UI guff.
            if (IsClientSide(examined))
                return true;

            return !Deleted(examined) && CanExamine(examiner, EntityManager.GetComponent<TransformComponent>(examined).MapPosition,
                entity => entity == examiner || entity == examined, examined);
        }

        [Pure]
        public virtual bool CanExamine(EntityUid examiner, MapCoordinates target, Ignored? predicate = null, EntityUid? examined = null, ExaminerComponent? examinerComp = null)
        {
            // TODO occluded container checks
            // also requires checking if the examiner has either a storage or stripping UI open, as the item may be accessible via that UI

            if (!Resolve(examiner, ref examinerComp, false))
                return false;

            // Ghosts and admins skip examine checks.
            if (examinerComp.SkipChecks)
                return true;

            if (examined != null)
            {
                var ev = new ExamineAttemptEvent(examiner);
                RaiseLocalEvent(examined.Value, ev);
                if (ev.Cancelled)
                    return false;
            }

            if (!examinerComp.CheckInRangeUnOccluded)
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
                if (MobStateSystem.IsDead(examiner, mobState))
                    return DeadExamineRange;

                if (MobStateSystem.IsCritical(examiner, mobState) || TryComp<BlindableComponent>(examiner, out var blind) && blind.IsBlind)
                    return CritExamineRange;

                if (TryComp<BlurryVisionComponent>(examiner, out var blurry))
                    return Math.Clamp(ExamineRange - blurry.Magnitude * ExamineBlurrinessMult, 2, ExamineRange);
            }
            return ExamineRange;
        }

        /// <summary>
        /// True if occluders are drawn for this entity, otherwise false.
        /// </summary>
        public bool IsOccluded(EntityUid uid)
        {
            return TryComp<EyeComponent>(uid, out var eye) && eye.DrawFov;
        }

        public static bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
        {
            // No, rider. This is better.
            // ReSharper disable once ConvertToLocalFunction
            var wrapped = (EntityUid uid, Ignored? wrapped)
                => wrapped != null && wrapped(uid);

            return InRangeUnOccluded(origin, other, range, predicate, wrapped, ignoreInsideBlocker, entMan);
        }

        public static bool InRangeUnOccluded<TState>(MapCoordinates origin, MapCoordinates other, float range,
            TState state, Func<EntityUid, TState, bool> predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
        {
            if (other.MapId != origin.MapId ||
                other.MapId == MapId.Nullspace) return false;

            var dir = other.Position - origin.Position;
            var length = dir.Length();

            // If range specified also check it
            // TODO: This rounding check is here because the API is kinda eh
            if (range > 0f && length > range + 0.01f) return false;

            if (MathHelper.CloseTo(length, 0)) return true;

            if (length > MaxRaycastRange)
            {
                Logger.Warning("InRangeUnOccluded check performed over extreme range. Limiting CollisionRay size.");
                length = MaxRaycastRange;
            }

            var occluderSystem = Get<OccluderSystem>();
            IoCManager.Resolve(ref entMan);

            var ray = new Ray(origin.Position, dir.Normalized());
            var rayResults = occluderSystem
                .IntersectRayWithPredicate(origin.MapId, ray, length, state, predicate, false).ToList();

            if (rayResults.Count == 0) return true;

            if (!ignoreInsideBlocker) return false;

            foreach (var result in rayResults)
            {
                if (!entMan.TryGetComponent(result.HitEntity, out OccluderComponent? o))
                {
                    continue;
                }

                var bBox = o.BoundingBox;
                bBox = bBox.Translated(entMan.GetComponent<TransformComponent>(result.HitEntity).WorldPosition);

                if (bBox.Contains(origin.Position) || bBox.Contains(other.Position))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool InRangeUnOccluded(EntityUid origin, EntityUid other, float range = ExamineRange, Ignored? predicate = null, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;
            var otherPos = entMan.GetComponent<TransformComponent>(other).MapPosition;

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(EntityUid origin, EntityCoordinates other, float range = ExamineRange, Ignored? predicate = null, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;
            var otherPos = other.ToMap(entMan);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public static bool InRangeUnOccluded(EntityUid origin, MapCoordinates other, float range = ExamineRange, Ignored? predicate = null, bool ignoreInsideBlocker = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var originPos = entMan.GetComponent<TransformComponent>(origin).MapPosition;

            return InRangeUnOccluded(originPos, other, range, predicate, ignoreInsideBlocker);
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
            RaiseLocalEvent(entity, examinedEvent, true);

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
            if (message.Nodes.Count == 0)
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

    /// <summary>
    ///     Event raised directed at an entity that someone is attempting to examine
    /// </summary>
    public sealed class ExamineAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Examiner;

        public ExamineAttemptEvent(EntityUid examiner)
        {
            Examiner = examiner;
        }
    }
}

using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Verbs
{
    [Serializable, NetSerializable]
    public class RequestServerVerbsEvent : EntityEventArgs
    {
        public readonly EntityUid EntityUid;

        public RequestServerVerbsEvent(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }
    }

    [Serializable, NetSerializable]
    public class VerbsResponseMessage : EntityEventArgs
    {
        public readonly Dictionary<string, Verb> Verbs;
        public readonly EntityUid Entity;

        public VerbsResponseMessage(Dictionary<string, Verb> verbs, EntityUid entity)
        {
            Verbs = verbs;
            Entity = entity;
        }
    }

    [Serializable, NetSerializable]
    public class UseVerbMessage : EntityEventArgs
    {
        public readonly EntityUid EntityUid;
        public readonly string VerbKey;

        public UseVerbMessage(EntityUid entityUid, string verbKey)
        {
            EntityUid = entityUid;
            VerbKey = verbKey;
        }
    }

    public class AssembleVerbsEvent : EntityEventArgs
    {
        /// <summary>
        ///     Event output. List of verbs that can be executed.
        /// </summary>
        public Dictionary<string, Verb> Verbs = new();

        /// <summary>
        ///     What kind of verbs to assemble. If this is null, includes all verbs.
        /// </summary>
        public InteractionType? Interaction;

        /// <summary>
        ///     Constant for determining whether the target verb is 'In Range' for physical interactions.
        /// </summary>
        public const float InteractionRangeSquared = 4;

        /// <summary>
        ///     Is the user in range of the target for physical interactions?
        /// </summary>
        public bool InRange;

        /// <summary>
        ///     The entity being targeted for the verb.
        /// </summary>
        public IEntity Target;

        /// <summary>
        ///     The entity that will be "performing" the verb.
        /// </summary>
        public IEntity User;

        /// <summary>
        ///     The entity currently being held by the active hand.
        /// </summary>
        /// <remarks>
        ///     If this is null, but the user has a HandsComponent, the hand is probably empty.
        /// </remarks>
        public IEntity? Using;

        /// <summary>
        ///     The User's hand component.
        /// </summary>
        public SharedHandsComponent? Hands;

        /// <summary>
        ///     Whether or not to load icons and string localizations in preparation for displaying in a GUI.
        /// </summary>
        public bool PrepareGUI;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">The user that will perform the verb.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="prepareGUI">Whether the verbs will be displayed in a GUI</param>
        /// <param name="interaction">The type of interactions to include as verbs.</param>
        public AssembleVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false, InteractionType? interaction = null)
        {
            Interaction = interaction;
            User = user;
            Target = target;
            PrepareGUI = prepareGUI;

            // Here we check if physical interactions are permitted. First, does the user have hands?
            if (!user.TryGetComponent<SharedHandsComponent>(out var hands))
                return;

            // Are physical interactions blocked somehow?
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                return;

            // Can the user physically access the target?
            if (!user.IsInSameOrParentContainer(target))
                return;

            // Physical interactions are allowed.
            Hands = hands;
            Hands.TryGetActiveHeldEntity(out Using);

            // Are they in range? Some verbs may not require this.
            var distanceSquared = (user.Transform.WorldPosition - target.Transform.WorldPosition).LengthSquared;
            InRange = distanceSquared <= InteractionRangeSquared;
        }
    }
}

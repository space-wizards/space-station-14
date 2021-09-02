using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.IoC;

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
        public readonly List<Verb>? Verbs;
        public readonly EntityUid Entity;

        public VerbsResponseMessage(List<Verb>? verbs, EntityUid entity)
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
        public List<Verb> Verbs = new();

        /// <summary>
        ///     What kind of verbs to assemble. Defaults to all verb types
        /// </summary>
        public VerbTypes Types;

        /// <summary>
        ///     Is the user in range and has obstructed access to the target?
        /// </summary>
        /// <remarks>
        ///     This is simply a cached <see cref="SharedUnobstructedExtensions.InRangeUnobstructed"/> result with the
        ///     default arguments, in order to avoid the function being called by every single system that wants to add a verb.
        /// </remarks>
        public bool DefaultInRangeUnobstructed;

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
        /// <remarks>
        ///     Avoids sending unnecessary data over the network.
        /// </remarks>
        public bool PrepareGUI;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">The user that will perform the verb.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="prepareGUI">Whether the verbs will be displayed in a GUI</param>
        /// <param name="types">The types of interactions to include as verbs.</param>
        public AssembleVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false,
            VerbTypes types = VerbTypes.All)
        {
            User = user;
            Target = target;
            PrepareGUI = prepareGUI;
            Types = types;

            // Because the majority of verbs need to check InRangeUnobstructed, cache it with default args.
            DefaultInRangeUnobstructed = this.InRangeUnobstructed();

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

            // If the "Held" entity is a virtual pull entity, consider the pulled entity as being used on the object
            if (Using != null && Using.TryGetComponent<HandVirtualPullComponent>(out var pull))
            {
                // Resolve entity uid
                Using = IoCManager.Resolve<IEntityManager>().GetEntity(pull.PulledEntity);
            }
        }
    }
}

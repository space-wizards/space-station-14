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
        public readonly VerbType Type;

        public RequestServerVerbsEvent(EntityUid entityUid, VerbType type)
        {
            EntityUid = entityUid;
            Type = type;
        }
    }

    [Serializable, NetSerializable]
    public class VerbsResponseEvent : EntityEventArgs
    {
        public readonly Dictionary<VerbType, List<Verb>>? Verbs;
        public readonly EntityUid Entity;

        public VerbsResponseEvent(EntityUid entity, Dictionary<VerbType, List<Verb>>? verbs)
        {
            Entity = entity;
            Verbs = verbs;
        }
    }

    [Serializable, NetSerializable]
    public class TryExecuteVerbEvent : EntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly string VerbKey;

        /// <summary>
        ///     The type of verb to try execute. Avoids having to get a list of all verbs on the receiving end.
        /// </summary>
        public readonly VerbType Type;

        public TryExecuteVerbEvent(EntityUid target, string verbKey, VerbType type)
        {
            Target = target;
            VerbKey = verbKey;
            Type = type;
        }
    }

    /// <summary>
    ///    Request primary interaction verbs.
    /// </summary>
    /// <remarks>
    ///    These verbs those that involve using the hands or the currently held item on some entity. These verbs usually
    ///    correspond to interactions that can be triggered by left-clicking or using 'Z', and often depend on the
    ///    currently held item. These verbs are collectively shown first in the context menu.
    /// </remarks>
    public class GetInteractionVerbsEvent : GetVerbsEvent
    {
        public GetInteractionVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false) : base(user, target, prepareGUI) { }
    }

    /// <summary>
    ///    Request activation verbs.
    /// </summary>
    /// <remarks>
    ///    These are verbs that activate an item in the world but are independent of the currently held items. For
    ///    example, opening a door or a GUI. These verbs should correspond to interactions that can be triggered by
    ///    using 'E', though many of those can also be triggered by left-mouse or 'Z' if there is no other interaction.
    ///    These verbs are collectively shown second in the context menu.
    /// </remarks>
    public class GetActivationVerbsEvent : GetVerbsEvent
    {
        public GetActivationVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false) : base(user, target, prepareGUI) { }
    }

    /// <summary>
    ///     Request alternative-interaction verbs.    
    /// </summary>
    /// <remarks>
    ///     When interacting with an entity via alt + left-click/E/Z the highest priority alt-interact verb is executed.
    ///     These verbs are collectively shown second-to-last in the context menu.
    /// </remarks>
    public class GetAlternativeVerbsEvent : GetVerbsEvent
    {
        public GetAlternativeVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false) : base(user, target, prepareGUI) { }
    }

    /// <summary>
    ///     Request Miscellaneous verbs.    
    /// </summary>
    /// <remarks>
    ///     Includes (nearly) global interactions like "examine", "pull", or "debug". These verbs are collectively shown
    ///     last in the context menu.
    /// </remarks>
    public class GetOtherVerbsEvent : GetVerbsEvent
    {
        public GetOtherVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false) : base(user, target, prepareGUI) { }
    }

    /// <summary>
    ///     Directed event that requests verbs from any systems/components on a target entity.
    /// </summary>
    public class GetVerbsEvent : EntityEventArgs
    {
        /// <summary>
        ///     Event output. List of verbs that can be executed.
        /// </summary>
        public List<Verb> Verbs = new();

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
        /// <remarks>
        ///     This may be null if the user has no hands.
        /// </remarks>
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
        public GetVerbsEvent(IEntity user, IEntity target, bool prepareGUI)
        {
            User = user;
            Target = target;
            PrepareGUI = prepareGUI;

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

            // Check whether the "Held" entity is a virtual pull entity. If yes, set that as the entity being "Used".
            // This allows you to do things like buckle a dragged person onto a surgery table, without click-dragging
            // their sprite.
            if (Using != null && Using.TryGetComponent<HandVirtualPullComponent>(out var pull))
            {
                // Resolve entity uid
                Using = IoCManager.Resolve<IEntityManager>().GetEntity(pull.PulledEntity);
            }
        }
    }
}

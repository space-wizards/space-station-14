using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.IoC;
using Content.Shared.Interaction;

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

        public VerbsResponseEvent(EntityUid entity, Dictionary<VerbType, SortedSet<Verb>>? verbs)
        {
            Entity = entity;

            if (verbs == null)
                return;

            // Apparently SortedSet is not serlializable. Cast to List<Verb>.
            Verbs = new();
            foreach (var entry in verbs)
            {
                Verbs.Add(entry.Key, new List<Verb>(entry.Value));
            }
        }
    }

    [Serializable, NetSerializable]
    public class ExecuteVerbEvent : EntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly Verb RequestedVerb;

        /// <summary>
        ///     The type of verb to try execute. Avoids having to get a list of all verbs on the receiving end.
        /// </summary>
        public readonly VerbType Type;

        public ExecuteVerbEvent(EntityUid target, Verb requestedVerb, VerbType type)
        {
            Target = target;
            RequestedVerb = requestedVerb;
            Type = type;
        }
    }

    /// <summary>
    ///    Request primary interaction verbs. This includes both use-in-hand and interacting with external entities.
    /// </summary>
    /// <remarks>
    ///    These verbs those that involve using the hands or the currently held item on some entity. These verbs usually
    ///    correspond to interactions that can be triggered by left-clicking or using 'Z', and often depend on the
    ///    currently held item. These verbs are collectively shown first in the context menu.
    /// </remarks>
    public class GetInteractionVerbsEvent : GetVerbsEvent
    {
        public GetInteractionVerbsEvent(IEntity user, IEntity target, bool force=false) : base(user, target, force) { }
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
        public GetActivationVerbsEvent(IEntity user, IEntity target, bool force=false) : base(user, target, force) { }
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
        public GetAlternativeVerbsEvent(IEntity user, IEntity target, bool force=false) : base(user, target, force) { }
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
        public GetOtherVerbsEvent(IEntity user, IEntity target, bool force=false) : base(user, target, force) { }
    }

    /// <summary>
    ///     Directed event that requests verbs from any systems/components on a target entity.
    /// </summary>
    public class GetVerbsEvent : EntityEventArgs
    {
        /// <summary>
        ///     Event output. Set of verbs that can be executed.
        /// </summary>
        public SortedSet<Verb> Verbs = new();

        /// <summary>
        ///     Can the user physically access the target?
        /// </summary>
        /// <remarks>
        ///     This is a combination of <see cref="ContainerHelpers.IsInSameOrParentContainer"/> and
        ///     <see cref="SharedInteractionSystem.InRangeUnobstructed"/>.
        /// </remarks>
        public bool CanAccess;

        /// <summary>
        ///     The entity being targeted for the verb.
        /// </summary>
        public IEntity Target;

        /// <summary>
        ///     The entity that will be "performing" the verb.
        /// </summary>
        public IEntity User;

        /// <summary>
        ///     Can the user physically interact?
        /// </summary>
        /// <remarks>
        ///     This is a just a cached <see cref="ActionBlockerSystem.CanInteract"/> result. Given that many verbs need
        ///     to check this, it prevents it from having to be repeatedly called by each individual system that might
        ///     contribute a verb.
        /// </remarks>
        public bool CanInteract;

        /// <summary>
        ///     The User's hand component.
        /// </summary>
        /// <remarks>
        ///     This may be null if the user has no hands.
        /// </remarks>
        public SharedHandsComponent? Hands;

        /// <summary>
        ///     The entity currently being held by the active hand.
        /// </summary>
        /// <remarks>
        ///     This is only ever not null when <see cref="ActionBlockerSystem.CanUse(EntityUid)"/> is true and the user
        ///     has hands.
        /// </remarks>
        public IEntity? Using;

        public GetVerbsEvent(IEntity user, IEntity target, bool force=false)
        {
            User = user;
            Target = target;

            CanAccess = force || (Target == User) || user.IsInSameOrParentContainer(target) &&
                EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(user, target);

            // A large number of verbs need to check action blockers. Instead of repeatedly having each system individually
            // call ActionBlocker checks, just cache it for the verb request.
            var actionBlockerSystem = EntitySystem.Get<ActionBlockerSystem>();
            CanInteract = force || actionBlockerSystem.CanInteract(user.Uid);

            if (!user.TryGetComponent(out Hands) ||
                !actionBlockerSystem.CanUse(user.Uid))
                return;

            Hands.TryGetActiveHeldEntity(out Using);

            // Check whether the "Held" entity is a virtual pull entity. If yes, set that as the entity being "Used".
            // This allows you to do things like buckle a dragged person onto a surgery table, without click-dragging
            // their sprite.
            if (Using != null && Using.TryGetComponent<HandVirtualItemComponent>(out var pull))
            {
                Using = IoCManager.Resolve<IEntityManager>().GetEntity(pull.BlockingEntity);
            }
        }
    }
}

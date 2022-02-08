using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Verbs
{
    public abstract class SharedVerbSystem : EntitySystem
    {
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeAllEvent<ExecuteVerbEvent>(HandleExecuteVerb);
        }

        private void HandleExecuteVerb(ExecuteVerbEvent args, EntitySessionEventArgs eventArgs)
        {
            var user = eventArgs.SenderSession.AttachedEntity;
            if (user == null)
                return;

            // Get the list of verbs. This effectively also checks that the requested verb is in fact a valid verb that
            // the user can perform.
            var verbs = GetLocalVerbs(args.Target, user.Value, args.Type)[args.Type];

            // Note that GetLocalVerbs might waste time checking & preparing unrelated verbs even though we know
            // precisely which one we want to run. However, MOST entities will only have 1 or 2 verbs of a given type.
            // The one exception here is the "other" verb type, which has 3-4 verbs + all the debug verbs.

            // Find the requested verb.
            if (verbs.TryGetValue(args.RequestedVerb, out var verb))
                ExecuteVerb(verb, user.Value, args.Target);
        }

        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s) defined in local systems. This
        ///     does not request verbs from the server.
        /// </summary>
        public virtual Dictionary<VerbType, SortedSet<Verb>> GetLocalVerbs(EntityUid target, EntityUid user, VerbType verbTypes, bool force = false)
        {
            Dictionary<VerbType, SortedSet<Verb>> verbs = new();

            // accessibility checks
            bool canAccess = false;
            if (force || target == user)
                canAccess = true;
            else if (EntityManager.EntityExists(target) && _interactionSystem.InRangeUnobstructed(user, target, ignoreInsideBlocker: true))
            {
                if (ContainerSystem.IsInSameOrParentContainer(user, target))
                    canAccess = true;
                else
                    // the item might be in a backpack that the user has open
                    canAccess = _interactionSystem.CanAccessViaStorage(user, target);
            }

            // A large number of verbs need to check action blockers. Instead of repeatedly having each system individually
            // call ActionBlocker checks, just cache it for the verb request.
            var canInteract = force || _actionBlockerSystem.CanInteract(user);

            EntityUid? @using = null;
            if (TryComp(user, out SharedHandsComponent? hands) && (force || _actionBlockerSystem.CanUse(user)))
            {
                hands.TryGetActiveHeldEntity(out @using);

                // Check whether the "Held" entity is a virtual pull entity. If yes, set that as the entity being "Used".
                // This allows you to do things like buckle a dragged person onto a surgery table, without click-dragging
                // their sprite.

                if (TryComp(@using, out HandVirtualItemComponent? pull))
                {
                    @using = pull.BlockingEntity;
                }
            }

            if ((verbTypes & VerbType.Interaction) == VerbType.Interaction)
            {
                GetInteractionVerbsEvent getVerbEvent = new(user, target, @using, hands, canInteract, canAccess);
                RaiseLocalEvent(target, getVerbEvent);
                verbs.Add(VerbType.Interaction, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Activation) == VerbType.Activation)
            {
                GetActivationVerbsEvent getVerbEvent = new(user, target, @using, hands, canInteract, canAccess);
                RaiseLocalEvent(target, getVerbEvent);
                verbs.Add(VerbType.Activation, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Alternative) == VerbType.Alternative)
            {
                GetAlternativeVerbsEvent getVerbEvent = new(user, target, @using, hands, canInteract, canAccess);
                RaiseLocalEvent(target, getVerbEvent);
                verbs.Add(VerbType.Alternative, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Other) == VerbType.Other)
            {
                GetOtherVerbsEvent getVerbEvent = new(user, target, @using, hands, canInteract, canAccess);
                RaiseLocalEvent(target, getVerbEvent);
                verbs.Add(VerbType.Other, getVerbEvent.Verbs);
            }

            return verbs;
        }

        /// <summary>
        ///     Execute the provided verb.
        /// </summary>
        /// <remarks>
        ///     This will try to call the action delegates and raise the local events for the given verb.
        /// </remarks>
        public abstract void ExecuteVerb(Verb verb, EntityUid user, EntityUid target, bool forced = false);
    }
}

using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Verbs
{
    public abstract class SharedVerbSystem : EntitySystem
    {
        [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

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
            else if (_interactionSystem.InRangeUnobstructed(user, target, ignoreInsideBlocker: true))
            {
                if (user.IsInSameOrParentContainer(target))
                    canAccess = true;
                else
                    // the item might be in a backpack that the user has open
                    canAccess = _interactionSystem.CanAccessViaStorage(user, target);
            }

            // A large number of verbs need to check action blockers. Instead of repeatedly having each system individually
            // call ActionBlocker checks, just cache it for the verb request.
            var canInteract = force || _actionBlockerSystem.CanInteract(user);

            EntityUid? @using = null;
            if (EntityManager.TryGetComponent(user, out SharedHandsComponent? hands) && (force || _actionBlockerSystem.CanUse(user)))
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
        public void ExecuteVerb(Verb verb, EntityUid user, EntityUid target, bool forced = false)
        {
            // first, lets log the verb. Just in case it ends up crashing the server or something.
            LogVerb(verb, user, target, forced);

            // then invoke any relevant actions
            verb.Act?.Invoke();

            // Maybe raise a local event
            if (verb.ExecutionEventArgs != null)
            {
                if (verb.EventTarget.IsValid())
                    RaiseLocalEvent(verb.EventTarget, verb.ExecutionEventArgs);
                else
                    RaiseLocalEvent(verb.ExecutionEventArgs);
            }
        }

        public void LogVerb(Verb verb, EntityUid user, EntityUid target, bool forced)
        {
            // first get the held item. again.
            EntityUid? usedUid = null;
            if (EntityManager.TryGetComponent(user, out SharedHandsComponent? hands) &&
                hands.TryGetActiveHeldEntity(out var heldEntity))
            {
                usedUid = heldEntity.Value;
                if (usedUid != default && EntityManager.TryGetComponent(usedUid, out HandVirtualItemComponent? pull))
                    usedUid = pull.BlockingEntity;
            }

            // get all the entities
            if (!user.IsValid() || !target.IsValid())
                return;

            EntityUid? used = null;
            if (usedUid != default)
                EntityManager.EntityExists(usedUid);

            var verbText = $"{verb.Category?.Text} {verb.Text}".Trim();

            // lets not frame people, eh?
            var executionText = forced ? "was forced to execute" : "executed";

            if (used == null)
            {
                _logSystem.Add(LogType.Verb, verb.Impact,
                        $"{ToPrettyString(user):user} {executionText} the [{verbText:verb}] verb targeting {ToPrettyString(target):target}");
            }
            else
            {
                _logSystem.Add(LogType.Verb, verb.Impact,
                       $"{ToPrettyString(user):user} {executionText} the [{verbText:verb}] verb targeting {ToPrettyString(target):target} while holding {ToPrettyString(used.Value):held}");
            }
                
        }
    }
}

using System.Collections.Generic;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Verbs
{
    public abstract class SharedVerbSystem : EntitySystem
    {
        [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;

        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s) defined in local systems. This
        ///     does not request verbs from the server.
        /// </summary>
        public virtual Dictionary<VerbType, SortedSet<Verb>> GetLocalVerbs(IEntity target, IEntity user, VerbType verbTypes, bool force = false)
        {
            Dictionary<VerbType, SortedSet<Verb>> verbs = new();

            if ((verbTypes & VerbType.Interaction) == VerbType.Interaction)
            {
                GetInteractionVerbsEvent getVerbEvent = new(user, target, force);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Interaction, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Activation) == VerbType.Activation)
            {
                GetActivationVerbsEvent getVerbEvent = new(user, target, force);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Activation, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Alternative) == VerbType.Alternative)
            {
                GetAlternativeVerbsEvent getVerbEvent = new(user, target, force);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Alternative, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Other) == VerbType.Other)
            {
                GetOtherVerbsEvent getVerbEvent = new(user, target, force);
                RaiseLocalEvent(target.Uid, getVerbEvent);
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

        public void LogVerb(Verb verb, EntityUid userUid, EntityUid targetUid, bool forced)
        {
            // first get the held item. again.
            EntityUid? usedUid = null;
            if (EntityManager.TryGetComponent(userUid, out SharedHandsComponent? hands))
            {
                hands.TryGetActiveHeldEntity(out var useEntityd);
                usedUid = useEntityd?.Uid;
                if (usedUid != null && EntityManager.TryGetComponent(usedUid.Value, out HandVirtualItemComponent? pull))
                    usedUid = pull.BlockingEntity;
            }

            // get all the entities
            if (!EntityManager.TryGetEntity(userUid, out var user) ||
                !EntityManager.TryGetEntity(targetUid, out var target))
                return;

            IEntity? used = null;
            if (usedUid != null)
                EntityManager.TryGetEntity(usedUid.Value, out used);

            // then prepare the basic log message body
            var verbText = $"{verb.Category?.Text} {verb.Text}".Trim();
            var logText = forced
                ? $"was forced to execute the '{verbText}' verb targeting " // let's not frame people, eh?
                : $"executed '{verbText}' verb targeting ";

            // then log with entity information
            if (used != null)
                _logSystem.Add(LogType.Verb, verb.Impact,
                       $"{user} {logText} {target} while holding {used}");
            else
                _logSystem.Add(LogType.Verb, verb.Impact,
                       $"{user} {logText} {target}");
        }
    }
}

using System.Collections.Generic;
using Content.Shared.Administration.Logs;
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
        public void ExecuteVerb(Verb verb)
        {
            if (verb.Using.HasValue)
                // note that not all verbs REQUIRE the user to use an item. This might lead to nonsense entries E.g. "examine using wooden planks".
                // but at least it will record what the user is holding as they perform some action.
                _logSystem.Add(LogType.Verb, verb.Impact,
                       $"{verb.User} executed '{verb.Category} {verb.Text}' verb on {verb.Target} using {verb.Using}");
            else
                _logSystem.Add(LogType.Verb, verb.Impact,
                       $"{verb.User} executed '{verb.Category} {verb.Text}' verb on {verb.Target}");

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
    }
}

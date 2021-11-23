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

            // each verb has it's own event & associated system subscriptions.
            // but we also don't want to have to call Action blocker again for each event type
            // so reuse the same event.
            var args = new GetVerbsEvent(user, target, force);

            if ((verbTypes & VerbType.Interaction) == VerbType.Interaction)
            {   
                RaiseLocalEvent(target.Uid, (GetInteractionVerbsEvent) args);
                verbs.Add(VerbType.Interaction, args.Verbs);
                args.Verbs.Clear();
            }

            if ((verbTypes & VerbType.Activation) == VerbType.Activation)
            {
                RaiseLocalEvent(target.Uid, (GetActivationVerbsEvent) args);
                verbs.Add(VerbType.Activation, args.Verbs);
                args.Verbs.Clear();
            }

            if ((verbTypes & VerbType.Alternative) == VerbType.Alternative)
            {
                RaiseLocalEvent(target.Uid, (GetAlternativeVerbsEvent) args);
                verbs.Add(VerbType.Alternative, args.Verbs);
                args.Verbs.Clear();
            }

            if ((verbTypes & VerbType.Other) == VerbType.Other)
            {
                RaiseLocalEvent(target.Uid, (GetOtherVerbsEvent) args);
                verbs.Add(VerbType.Other, args.Verbs);
                args.Verbs.Clear();
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

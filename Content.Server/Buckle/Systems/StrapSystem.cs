using Content.Server.Buckle.Components;
using Content.Server.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System.Collections.Generic;

namespace Content.Server.Buckle.Systems
{
    [UsedImplicitly]
    internal sealed class StrapSystem : EntitySystem
    {
        [Dependency] InteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrapComponent, GetInteractionVerbsEvent>(AddStrapVerbs);
        }

        // TODO ECS BUCKLE/STRAP These 'Strap' verbs are an incestuous mess of buckle component and strap component
        // functions. Whenever these are fully ECSed, maybe do it in a way that allows for these verbs to be handled in
        // a sensible manner in a single system?

        private void AddStrapVerbs(EntityUid uid, StrapComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // Note that for whatever bloody reason, buckle component has its own interaction range. Additionally, this
            // range can be set per-component, so we have to check a modified InRangeUnobstructed for every verb.

            // Add unstrap verbs for every strapped entity.
            foreach (var entity in component.BuckledEntities)
            {
                var buckledComp = entity.GetComponent<BuckleComponent>();

                if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target, range: buckledComp.Range))
                    continue;

                Verb verb = new();
                verb.Act = () => buckledComp.TryUnbuckle(args.User);
                verb.Category = VerbCategory.Unbuckle;
                if (entity == args.User)
                    verb.Text = Loc.GetString("verb-self-target-pronoun");
                else
                    verb.Text = entity.Name;

                // In the event that you have more than once entity with the same name strapped to the same object,
                // these two verbs will be identical according to Verb.CompareTo, and only one with actually be added to
                // the verb list. However this should rarely ever be a problem. If it ever is, it could be fixed by
                // appending an integer to verb.Text to distinguish the verbs.

                args.Verbs.Add(verb);
            }

            // Add a verb to buckle the user.
            if (args.User.TryGetComponent<BuckleComponent>(out var buckle) &&
                buckle.BuckledTo != component &&
                args.User != component.Owner &&
                component.HasSpace(buckle) &&
                _interactionSystem.InRangeUnobstructed(args.User, args.Target, range: buckle.Range))
            {
                Verb verb = new();
                verb.Act = () => buckle.TryBuckle(args.User, args.Target);
                verb.Category = VerbCategory.Buckle;
                verb.Text = Loc.GetString("verb-self-target-pronoun");
                args.Verbs.Add(verb);
            }

            // If the user is currently holding/pulling an entity that can be buckled, add a verb for that.
            if (args.Using != null &&
                args.Using.TryGetComponent<BuckleComponent>(out var usingBuckle) &&
                component.HasSpace(usingBuckle) &&
                _interactionSystem.InRangeUnobstructed(args.Using, args.Target, range: usingBuckle.Range))
            {
                // Check that the entity is unobstructed from the target (ignoring the user).
                bool Ignored(IEntity entity) => entity == args.User || entity == args.Target || entity == args.Using;
                if (!_interactionSystem.InRangeUnobstructed(args.Using, args.Target, usingBuckle.Range, predicate: Ignored))
                    return;

                Verb verb = new();
                verb.Act = () => usingBuckle.TryBuckle(args.User, args.Target);
                verb.Category = VerbCategory.Buckle;
                verb.Text = args.Using.Name;

                // If the used entity is a person being pulled, prioritize this verb. Conversely, if it is
                // just a held object, the user is probably just trying to sit down.
                verb.Priority = args.Using.HasComponent<ActorComponent>() ? 1 : -1;

                args.Verbs.Add(verb);
            }
        }
    }
}

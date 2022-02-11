using Content.Server.Buckle.Components;
using Content.Server.Interaction;
using Content.Shared.Body.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Buckle.Systems
{
    [UsedImplicitly]
    internal sealed class StrapSystem : EntitySystem
    {
        [Dependency] InteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrapComponent, GetVerbsEvent<InteractionVerb>>(AddStrapVerbs);
            SubscribeLocalEvent<StrapComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        }

        private void OnInsertAttempt(EntityUid uid, StrapComponent component, ContainerGettingInsertedAttemptEvent args)
        {
            // If someone is attempting to put this item inside of a backpack, ensure that it has no entities strapped to it.
            if (HasComp<SharedStorageComponent>(args.Container.Owner) && component.BuckledEntities.Count != 0)
                args.Cancel();
        }

        // TODO ECS BUCKLE/STRAP These 'Strap' verbs are an incestuous mess of buckle component and strap component
        // functions. Whenever these are fully ECSed, maybe do it in a way that allows for these verbs to be handled in
        // a sensible manner in a single system?

        private void AddStrapVerbs(EntityUid uid, StrapComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || !component.Enabled)
                return;

            // Note that for whatever bloody reason, buckle component has its own interaction range. Additionally, this
            // range can be set per-component, so we have to check a modified InRangeUnobstructed for every verb.

            // Add unstrap verbs for every strapped entity.
            foreach (var entity in component.BuckledEntities)
            {
                var buckledComp = EntityManager.GetComponent<BuckleComponent>(entity);

                if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target, range: buckledComp.Range))
                    continue;

                InteractionVerb verb = new()
                {
                    Act = () => buckledComp.TryUnbuckle(args.User),
                    Category = VerbCategory.Unbuckle
                };

                if (entity == args.User)
                    verb.Text = Loc.GetString("verb-self-target-pronoun");
                else
                    verb.Text = EntityManager.GetComponent<MetaDataComponent>(entity).EntityName;

                // In the event that you have more than once entity with the same name strapped to the same object,
                // these two verbs will be identical according to Verb.CompareTo, and only one with actually be added to
                // the verb list. However this should rarely ever be a problem. If it ever is, it could be fixed by
                // appending an integer to verb.Text to distinguish the verbs.

                args.Verbs.Add(verb);
            }

            // Add a verb to buckle the user.
            if (EntityManager.TryGetComponent<BuckleComponent?>(args.User, out var buckle) &&
                buckle.BuckledTo != component &&
                args.User != component.Owner &&
                component.HasSpace(buckle) &&
                _interactionSystem.InRangeUnobstructed(args.User, args.Target, range: buckle.Range))
            {
                InteractionVerb verb = new()
                {
                    Act = () => buckle.TryBuckle(args.User, args.Target),
                    Category = VerbCategory.Buckle,
                    Text = Loc.GetString("verb-self-target-pronoun")
                };
                args.Verbs.Add(verb);
            }

            // If the user is currently holding/pulling an entity that can be buckled, add a verb for that.
            if (args.Using is {Valid: true} @using &&
                EntityManager.TryGetComponent<BuckleComponent?>(@using, out var usingBuckle) &&
                component.HasSpace(usingBuckle) &&
                _interactionSystem.InRangeUnobstructed(@using, args.Target, range: usingBuckle.Range))
            {
                // Check that the entity is unobstructed from the target (ignoring the user).
                bool Ignored(EntityUid entity) => entity == args.User || entity == args.Target || entity == @using;
                if (!_interactionSystem.InRangeUnobstructed(@using, args.Target, usingBuckle.Range, predicate: Ignored))
                    return;

                InteractionVerb verb = new()
                {
                    Act = () => usingBuckle.TryBuckle(args.User, args.Target),
                    Category = VerbCategory.Buckle,
                    Text = EntityManager.GetComponent<MetaDataComponent>(@using).EntityName,
                    // just a held object, the user is probably just trying to sit down.
                    // If the used entity is a person being pulled, prioritize this verb. Conversely, if it is
                    Priority = EntityManager.HasComponent<ActorComponent>(@using) ? 1 : -1
                };

                args.Verbs.Add(verb);
            }
        }
    }
}

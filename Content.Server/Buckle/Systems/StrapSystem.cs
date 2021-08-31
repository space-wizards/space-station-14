using Content.Server.Buckle.Components;
using Content.Server.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Buckle.Systems
{
    [UsedImplicitly]
    internal sealed class StrapSystem : EntitySystem
    {
        [Dependency] IEntityManager _entityManager = default!;
        [Dependency] InteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrapComponent, AssembleVerbsEvent>(AddStrapVerb);
        }

        // TODO ECS BUCKLE/STRAP
        // These 'Strap' verbs are an incestuous mess of buckle component and strap component functions.
        // Whenever these are fully ECSed, maybe do it in a way that allows for these verbs to be done in a sensible manner?

        /// <summary>
        ///     Unstrap a buckle-able entity from another entity. Similar functionality to unbuckling, except here the
        ///     targeted entity is the one that the other entity is strapped to (e.g., a hospital bed).
        /// </summary>
        private void AddStrapVerb(EntityUid uid, StrapComponent component, AssembleVerbsEvent args)
        {
            if (!args.Types.HasFlag(VerbTypes.Interact))
                return;

            // Can the user interact?
            if (!args.DefaultInRangeUnobstructed || args.Hands == null)
                return;
            // Note that for whatever bloody reason, every buckle component has its own interaction range, so we have to
            // check a modified InRangeUnobstructed for every verb.

            // Add unstrap verbs for every strapped entity.
            foreach (var entity in component.BuckledEntities)
            {
                var buckledComp = entity.GetComponent<BuckleComponent>();
                if (!args.InRangeUnobstructed(range: buckledComp.Range))
                    continue;

                Verb verb = new("unbuckle:"+entity.Uid.ToString());
                verb.Act = () => buckledComp.TryUnbuckle(args.User);
                if (args.PrepareGUI)
                {
                    verb.Category = VerbCategories.Unbuckle;
                    if (entity == args.User)
                        verb.Text = Loc.GetString("verb-self-target-pronoun");
                    else
                        verb.Text = entity.Name;
                }
                args.Verbs.Add(verb);
            }

            // Add a verb to buckle the user.
            if (args.User.TryGetComponent<BuckleComponent>(out var buckle) &&
                buckle.BuckledTo != component &&
                args.User != component.Owner &&
                component.HasSpace(buckle) &&
                args.InRangeUnobstructed(range: buckle.Range))
            {
                Verb verb = new("buckle:self");
                verb.Act = () => buckle.TryBuckle(args.User, args.Target);
                if (args.PrepareGUI)
                {
                    verb.Category = VerbCategories.Buckle;
                    verb.Text = Loc.GetString("verb-self-target-pronoun");
                }
                args.Verbs.Add(verb);
            }


            // Check if the user is currently pulling an entity that can be buckled to the target?
            // Damn this is one ugly if block.
            if (args.Using != null &&
                args.Using.TryGetComponent<HandVirtualPullComponent>(out var virtualPull) &&
                _entityManager.TryGetEntity(virtualPull.PulledEntity, out var pulledEntity) &&
                pulledEntity.TryGetComponent<BuckleComponent>(out var pulledBuckle) &&
                component.HasSpace(pulledBuckle) &&
                args.InRangeUnobstructed(range: pulledBuckle.Range, userOverride: pulledEntity))
            {
                // Check that the pulled entity is obstructed from the target (ignoring the user/puller).
                bool Ignored(IEntity entity) => entity == args.User || entity == args.Target || entity == pulledEntity;
                if (!_interactionSystem.InRangeUnobstructed(pulledEntity, args.Target, pulledBuckle.Range, predicate: Ignored))
                    return;

                // Add a verb to buckle the pulled entity
                Verb verb = new("buckle:pulled");
                verb.Act = () => pulledBuckle.TryBuckle(args.User, args.Target);
                if (args.PrepareGUI)
                {
                    verb.Category = VerbCategories.Buckle;
                    verb.Text = pulledEntity.Name;
                }
                args.Verbs.Add(verb);
            }
        }
    }
}

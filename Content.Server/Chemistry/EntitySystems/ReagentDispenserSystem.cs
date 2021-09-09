using Content.Shared.Verbs;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, SolutionChangedEvent>(OnSolutionChange);

            SubscribeLocalEvent<ReagentDispenserComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<ReagentDispenserComponent, GetInteractionVerbsEvent>(AddInsertVerb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system? Maybe using something like the
        // system mentioned in #4538? The code here is basically identical to the stuff in ChemDispenserSystem
        private void AddEjectVerb(EntityUid uid, ReagentDispenserComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.DefaultInRangeUnobstructed || args.Hands == null || !component.HasBeaker)
                return;

            Verb verb = new("ReagentDispenser:Eject");
            verb.Act = () =>
            {
                component.TryEject(args.User);
                component.UpdateUserInterface();
            };

            if (args.PrepareGUI)
            {
                verb.Category = VerbCategory.Eject;
            }

            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, ReagentDispenserComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.DefaultInRangeUnobstructed || args.Using == null || component.HasBeaker )
                return;

            if (!args.Using.HasComponent<FitsInDispenserComponent>() ||
                !_solutionContainerSystem.TryGetSolution(args.Using.Uid, "beaker", out _))
            {
                return;
            }

            Verb verb = new("ReagentDispenser:Insert");
            verb.Act = () =>
            {
                component.BeakerContainer.Insert(args.Using);
                component.UpdateUserInterface();
            };

            if (args.PrepareGUI)
            {
                verb.Category = VerbCategory.Insert;
            }

            args.Verbs.Add(verb);
        }


        private void OnSolutionChange(EntityUid uid, ReagentDispenserComponent component, SolutionChangedEvent args)
        {
            component.UpdateUserInterface();
        }
    }
}

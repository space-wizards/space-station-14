using Content.Shared.Verbs;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server.Chemistry.EntitySystems
{
	[UsedImplicitly]
    public class ChemMasterSystem : EntitySystem
    {
        public override void Initialize()
        {
			base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<ChemMasterComponent, GetInteractionVerbsEvent>(AddInsertVerb);
            SubscribeLocalEvent<ChemMasterComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system? Maybe using something like the
        // system mentioned in #4538? The code here is basically identical to the stuff in ChemDispenserSystem
        private void AddEjectVerb(EntityUid uid, ChemMasterComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || args.Hands == null || !component.HasBeaker)
                return;

            Verb verb = new("ChemMaster:Eject");
            verb.Act = () =>
            {
                component.TryEject(args.User);
                component.UpdateUserInterface();
            };
            verb.Category = VerbCategory.Eject;
            args.Verbs.Add(verb);
        }




        private void AddInsertVerb(EntityUid uid, ChemMasterComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || args.Using == null || component.HasBeaker)
                return;

            if (!args.Using.HasComponent<FitsInDispenserComponent>())
                return;

            Verb verb = new("ChemMaster:Insert");
            verb.Act = () =>
            {
                component.BeakerContainer.Insert(args.Using);
                component.UpdateUserInterface();
            };
            verb.Category = VerbCategory.Insert;
            args.Verbs.Add(verb);
        }
		
        private void OnSolutionChange(EntityUid uid, ChemMasterComponent component,
            SolutionChangedEvent solutionChanged)
        {
            component.UpdateUserInterface();
        }
	}
}
